using Microsoft.Extensions.Logging;
using PaymentService.Domain;

namespace PaymentService.Application;

public class PaymentAppService(
    IPaymentRepository repo,
    IPaymentProcessorFactory processors,
    IEventPublisher events,
    ILogger<PaymentAppService> log)
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
        if (existing != null) return (existing, false);

        // 3. Mỗi chuyến chỉ được thanh toán thành công một lần
        if (await repo.HasSucceededForTripAsync(req.TripId, ct))
            throw new PaymentException(409, "Chuyến đi này đã được thanh toán.");

        // 4. Lưu giao dịch ở trạng thái Pending
        var payment = Payment.Create(req.TripId, req.RiderId, req.Amount,
            req.Currency ?? "VND", req.Method, idempotencyKey);
        try
        {
            await repo.AddAsync(payment, ct);
        }
        catch (DuplicateIdempotencyKeyException)   // 2 request cùng key đến cùng lúc
        {
            var raced = await repo.GetByIdempotencyKeyAsync(idempotencyKey, ct);
            return (raced!, false);
        }

        // 5. Gọi cổng thanh toán
        GatewayResult result;
        try
        {
            result = await processors.Create(req.Method)
                .ChargeAsync(payment.Amount, payment.Currency, req.PaymentToken, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Lỗi gọi cổng thanh toán cho payment {PaymentId}", payment.Id);
            result = new GatewayResult(false, "Lỗi kết nối cổng thanh toán.");
        }

        // 6. Cập nhật trạng thái + ghi sổ cái trong CÙNG một transaction (1 SaveChanges)
        if (result.Success) payment.MarkSucceeded();
        else payment.MarkFailed(result.Error ?? "Thanh toán thất bại.");
        await repo.SaveChangesAsync(ct);

        // 7. Phát sự kiện cho Trip Service (sau này: Outbox + RabbitMQ)
        await events.PublishAsync(
            result.Success ? "PaymentSettled" : "PaymentFailed",
            new { PaymentId = payment.Id, payment.TripId, payment.Amount }, ct);

        return (payment, true);
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

    public async Task<Payment> RefundAsync(Guid id, CancellationToken ct)
    {
        var payment = await GetAsync(id, ct);
        if (payment.Status == PaymentStatus.Refunded) return payment;   // idempotent

        var result = await processors.Create(payment.Method)
            .RefundAsync(payment.Amount, payment.Currency, ct);
        if (!result.Success)
            throw new PaymentException(502, result.Error ?? "Cổng thanh toán từ chối hoàn tiền.");

        payment.Refund();               // ném InvalidOperationException nếu chưa Succeeded -> 409
        await repo.SaveChangesAsync(ct);
        await events.PublishAsync("PaymentRefunded", new { PaymentId = payment.Id, payment.TripId }, ct);
        return payment;
    }
}
