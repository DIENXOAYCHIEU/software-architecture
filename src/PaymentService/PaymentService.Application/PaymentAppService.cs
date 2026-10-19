using Microsoft.Extensions.Logging;
using PaymentService.Domain;

namespace PaymentService.Application;

public class PaymentAppService(
    IPaymentRepository repo,
    IPaymentProcessorFactory processors,
    IEventPublisher events,
    ILogger<PaymentAppService> log,
    IPaymentTokenProvider tokens,
    ReliabilityOptions reliability,
    ICorrelationIdAccessor correlation)
{
    /// <returns>(payment, created). created=false nghĩa là request lặp lại (idempotent replay).</returns>
    public async Task<(Payment Payment, bool Created)> CreateAsync(
        CreatePaymentRequest req, string? idempotencyKey, CancellationToken ct)
    {
        // 1. Validate
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new PaymentException(400, "Thiếu header Idempotency-Key.");
        if (req.TripId == Guid.Empty || req.RiderId == Guid.Empty)
            throw new PaymentException(400, "TripId và RiderId là bắt buộc.");
        if (req.Amount <= 0)
            throw new PaymentException(400, "Amount phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(req.PaymentToken))
            throw new PaymentException(400, "PaymentToken là bắt buộc.");

        // 2. Idempotency: cùng key => trả lại kết quả cũ, không trừ tiền lần nữa
        var existing = await repo.GetByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existing != null)
        {
            if (!string.Equals(existing.RequestFingerprint, req.ComputeFingerprint(), StringComparison.Ordinal))
                throw new PaymentException(409, "Idempotency-Key đã được dùng cho payload khác.");
            return (existing, false);
        }

        // 3. Mỗi chuyến chỉ được thanh toán thành công một lần
        if (await repo.HasSucceededForTripAsync(req.TripId, ct))
            throw new PaymentException(409, "Chuyến đi này đã được thanh toán.");

        // 4. Lưu giao dịch ở trạng thái Pending
        var payment = Payment.Create(req.TripId, req.RiderId, req.Amount,
            req.Currency?.Trim().ToUpperInvariant() ?? "VND",
            req.Method, idempotencyKey, req.ComputeFingerprint());
        try
        {
            await repo.AddAsync(payment, ct);
            await tokens.StoreTokenAsync(payment, req.PaymentToken, ct);
        }
        catch (DuplicateIdempotencyKeyException)   // 2 request cùng key đến cùng lúc
        {
            var raced = await repo.GetByIdempotencyKeyAsync(idempotencyKey, ct);
            if (raced is null)
                throw;
            if (!string.Equals(raced.RequestFingerprint, req.ComputeFingerprint(), StringComparison.Ordinal))
                throw new PaymentException(409, "Idempotency-Key đã được dùng cho payload khác.");
            return (raced, false);
        }

        await ProcessPaymentAsync(payment, req.Method, req.PaymentToken, ct);

        return (payment, true);
    }

    public async Task RetryPendingAsync(Payment payment, CancellationToken ct)
    {
        var token = await tokens.GetTokenAsync(payment, ct);
        if (string.IsNullOrWhiteSpace(token))
        {
            log.LogWarning("No payment token available for pending payment {PaymentId}", payment.Id);
            return;
        }

        await ProcessPaymentAsync(payment, payment.Method, token, ct);
    }

    private async Task ProcessPaymentAsync(
        Payment payment, PaymentMethod method, string token, CancellationToken ct)
    {
        var processor = processors.Create(method);
        GatewayResult? result = null;
        var remainingAttempts = reliability.MaxTotalAttempts - payment.AttemptCount;
        if (remainingAttempts <= 0)
        {
            payment.MarkFailed("Đã vượt quá số lần thử thanh toán cho phép.");
            await repo.SaveChangesAsync(ct);
            await PublishEventAsync("PaymentFailed", payment, ct);
            return;
        }

        var attempts = Math.Min(reliability.MaxGatewayAttempts, remainingAttempts);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            payment.MarkAttemptStarted();
            try
            {
                using var timeout = CancellationTokenSource
                    .CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(reliability.GatewayTimeoutSeconds));
                result = await processor.ChargeAsync(
                    payment.Amount, payment.Currency, token, timeout.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                result = new GatewayResult(
                    false, "Cổng thanh toán quá thời gian phản hồi.",
                    GatewayFailureKind.Transient);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Gateway error for payment {PaymentId}", payment.Id);
                result = new GatewayResult(
                    false, "Lỗi kết nối cổng thanh toán.",
                    GatewayFailureKind.Transient);
            }

            if (result.Success || result.FailureKind == GatewayFailureKind.Permanent)
                break;

            if (attempt + 1 < reliability.MaxGatewayAttempts)
                await Task.Delay(GetRetryDelay(attempt), ct);
        }

        if (result?.Success == true)
        {
            payment.MarkSucceeded();
            await PublishEventAsync("PaymentSettled", payment, ct);
            await repo.SaveChangesAsync(ct);
            return;
        }

        var error = result?.Error ?? "Thanh toán thất bại.";
        if (result?.FailureKind == GatewayFailureKind.Transient &&
            payment.AttemptCount < reliability.MaxTotalAttempts)
        {
            payment.MarkRetryPending(error, DateTime.UtcNow.Add(GetRetryDelay(payment.AttemptCount)));
            await repo.SaveChangesAsync(ct);
            log.LogWarning("Payment {PaymentId} remains pending for retry", payment.Id);
            return;
        }

        payment.MarkFailed(error);
        await PublishEventAsync("PaymentFailed", payment, ct);
        await repo.SaveChangesAsync(ct);
    }

    private async Task PublishEventAsync(string eventName, Payment payment, CancellationToken ct)
    {
        using var scope = log.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlation.Current ?? "none",
            ["PaymentId"] = payment.Id
        });
        await events.PublishAsync(
            eventName,
            new { PaymentId = payment.Id, payment.TripId, payment.Amount, CorrelationId = correlation.Current },
            ct);
    }

    private TimeSpan GetRetryDelay(int attempt)
    {
        var exponential = reliability.RetryBaseDelayMilliseconds * Math.Pow(2, Math.Max(0, attempt));
        var capped = Math.Min(exponential, reliability.RetryMaxDelayMilliseconds);
        var jitter = Random.Shared.NextDouble() * Math.Max(1, capped * 0.2);
        return TimeSpan.FromMilliseconds(capped + jitter);
    }

    public async Task<Payment> GetAsync(Guid id, CancellationToken ct) =>
        await repo.GetByIdAsync(id, ct) ?? throw new PaymentException(404, "Không tìm thấy giao dịch.");

    public Task<List<Payment>> GetByTripAsync(Guid tripId, CancellationToken ct) =>
        repo.GetByTripAsync(tripId, ct);

    public async Task<List<LedgerEntryResponse>> GetLedgerAsync(Guid paymentId, CancellationToken ct)
    {
        await GetAsync(paymentId, ct);
        var entries = await repo.GetLedgerAsync(paymentId, ct);
        return entries.Select(e => new LedgerEntryResponse(
            e.Id, e.Account, e.DebitAmount, e.CreditAmount, e.Type, e.CreatedAt)).ToList();
    }

    public async Task<Payment> RefundAsync(Guid id, string? refundIdempotencyKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refundIdempotencyKey))
            throw new PaymentException(400, "Thiếu header Idempotency-Key cho refund.");

        var payment = await GetAsync(id, ct);
        if (payment.Status == PaymentStatus.Refunded)
        {
            if (!string.Equals(payment.RefundIdempotencyKey, refundIdempotencyKey, StringComparison.Ordinal))
                throw new PaymentException(409, "Giao dịch đã được hoàn tiền với idempotency key khác.");
            return payment;
        }
        if (payment.Status != PaymentStatus.Succeeded)
            throw new PaymentException(409, "Chỉ giao dịch đã thanh toán thành công mới được hoàn tiền.");

        var result = await processors.Create(payment.Method)
            .RefundAsync(payment.Amount, payment.Currency, ct);
        if (!result.Success)
            throw new PaymentException(502, result.Error ?? "Cổng thanh toán từ chối hoàn tiền.");

        payment.Refund(refundIdempotencyKey); // ném InvalidOperationException nếu chưa Succeeded -> 409
        await events.PublishAsync("PaymentRefunded", new { PaymentId = payment.Id, payment.TripId }, ct);
        await repo.SaveChangesAsync(ct);
        return payment;
    }
}
