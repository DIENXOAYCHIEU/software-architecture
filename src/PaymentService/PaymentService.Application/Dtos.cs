using PaymentService.Domain;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PaymentService.Application;

public record CreatePaymentRequest(
    Guid TripId,
    Guid RiderId,
    decimal Amount,
    PaymentMethod Method,
    string PaymentToken,     // token do cổng thanh toán cấp, KHÔNG lưu số thẻ/CVV
    string? Currency = "VND")
{
    public string ComputeFingerprint()
    {
        var payload = string.Join("|",
            TripId.ToString("D"),
            RiderId.ToString("D"),
            Amount.ToString("0.00", CultureInfo.InvariantCulture),
            Method,
            PaymentToken,
            (Currency ?? "VND").Trim().ToUpperInvariant());

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}

public record LedgerEntryResponse(
    Guid Id, string Account, decimal DebitAmount, decimal CreditAmount, LedgerEntryType Type, DateTime CreatedAt);

public record PaymentResponse(
    Guid Id, Guid TripId, Guid RiderId, decimal Amount, string Currency,
    PaymentMethod Method, PaymentStatus Status, string? FailureReason,
    DateTime CreatedAt, DateTime UpdatedAt, int AttemptCount, DateTime? NextRetryAtUtc)
{
    public static PaymentResponse From(Payment p) => new(
        p.Id, p.TripId, p.RiderId, p.Amount, p.Currency,
        p.Method, p.Status, p.FailureReason, p.CreatedAt, p.UpdatedAt,
        p.AttemptCount, p.NextRetryAtUtc);
}
