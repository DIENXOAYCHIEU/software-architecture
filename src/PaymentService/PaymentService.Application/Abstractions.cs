using PaymentService.Domain;

namespace PaymentService.Application;

public record GatewayResult(bool Success, string? Error = null);

/// <summary>Cổng thanh toán (Card / Wallet). Trong đồ án dùng bản giả lập.</summary>
public interface IPaymentProcessor
{
    Task<GatewayResult> ChargeAsync(decimal amount, string currency, string token, CancellationToken ct);
    Task<GatewayResult> RefundAsync(decimal amount, string currency, CancellationToken ct);
}

/// <summary>Factory pattern: chọn processor theo PaymentMethod.</summary>
public interface IPaymentProcessorFactory
{
    IPaymentProcessor Create(PaymentMethod method);
}

/// <summary>Publisher sự kiện (PaymentSettled, PaymentFailed...). Sau này thay bằng RabbitMQ/MassTransit.</summary>
public interface IEventPublisher
{
    Task PublishAsync(string eventName, object payload, CancellationToken ct = default);
}

/// <summary>Repository pattern: Application không biết gì về EF Core / MySQL.</summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Payment?> GetByIdempotencyKeyAsync(string key, CancellationToken ct);
    Task<bool> HasSucceededForTripAsync(Guid tripId, CancellationToken ct);
    Task<List<Payment>> GetByTripAsync(Guid tripId, CancellationToken ct);
    Task<List<LedgerEntry>> GetLedgerAsync(Guid paymentId, CancellationToken ct);

    /// <exception cref="DuplicateIdempotencyKeyException">Trùng Idempotency-Key (race condition).</exception>
    Task AddAsync(Payment payment, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public class DuplicateIdempotencyKeyException : Exception { }

/// <summary>Lỗi nghiệp vụ kèm HTTP status code.</summary>
public class PaymentException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
