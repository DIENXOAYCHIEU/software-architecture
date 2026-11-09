using PaymentService.Application;
using PaymentService.Domain;

namespace PaymentService.Infrastructure;

public sealed class AuditTrail
{
    public long Id { get; set; }
    public Guid PaymentId { get; set; }
    public required string Action { get; set; }
    public required string Status { get; set; }
    public string? Details { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AuditTrailWriter(PaymentDbContext db) : IAuditTrailWriter
{
    public Task RecordAsync(Guid paymentId, string action, PaymentStatus status,
        string? details, string? correlationId, CancellationToken ct)
    {
        db.AuditTrails.Add(new AuditTrail
        {
            PaymentId = paymentId,
            Action = action,
            Status = status.ToString(),
            Details = details,
            CorrelationId = correlationId
        });
        return Task.CompletedTask;
    }
}
