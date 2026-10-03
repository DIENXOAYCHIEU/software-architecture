namespace PaymentService.Domain;

public enum PaymentMethod { Card, Wallet }
public enum PaymentStatus { Pending, Succeeded, Failed, Refunded }
public enum LedgerEntryType { Charge, Refund }

/// <summary>Giao dịch thanh toán của một chuyến đi (aggregate root).</summary>
public class Payment
{
    public const string PlatformAccount = "platform:revenue";

    private Payment() { }   // dành cho EF Core

    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public Guid RiderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; } = "";
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public List<LedgerEntry> LedgerEntries { get; } = new();

    public static Payment Create(Guid tripId, Guid riderId, decimal amount,
        string currency, PaymentMethod method, string idempotencyKey)
    {
        var now = DateTime.UtcNow;
        return new Payment
        {
            Id = Guid.NewGuid(), TripId = tripId, RiderId = riderId,
            Amount = amount, Currency = currency, Method = method,
            IdempotencyKey = idempotencyKey, Status = PaymentStatus.Pending,
            CreatedAt = now, UpdatedAt = now
        };
    }

    // Sổ cái double-entry: mỗi giao dịch luôn có 1 bút toán Nợ và 1 bút toán Có bằng nhau.
    public void MarkSucceeded()
    {
        EnsureStatus(PaymentStatus.Pending);
        Status = PaymentStatus.Succeeded;
        UpdatedAt = DateTime.UtcNow;
        LedgerEntries.Add(LedgerEntry.Debit(Id, $"rider:{RiderId}", Amount, LedgerEntryType.Charge));
        LedgerEntries.Add(LedgerEntry.Credit(Id, PlatformAccount, Amount, LedgerEntryType.Charge));
    }

    public void MarkFailed(string reason)
    {
        EnsureStatus(PaymentStatus.Pending);
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Refund()
    {
        EnsureStatus(PaymentStatus.Succeeded);
        Status = PaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
        // Bút toán đảo (compensation)
        LedgerEntries.Add(LedgerEntry.Debit(Id, PlatformAccount, Amount, LedgerEntryType.Refund));
        LedgerEntries.Add(LedgerEntry.Credit(Id, $"rider:{RiderId}", Amount, LedgerEntryType.Refund));
    }

    private void EnsureStatus(PaymentStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException(
                $"Không thể thực hiện thao tác: giao dịch đang ở trạng thái {Status}, cần {expected}.");
    }
}

public class LedgerEntry
{
    private LedgerEntry() { }

    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public string Account { get; private set; } = "";
    public decimal DebitAmount { get; private set; }
    public decimal CreditAmount { get; private set; }
    public LedgerEntryType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static LedgerEntry Debit(Guid paymentId, string account, decimal amount, LedgerEntryType type) =>
        new() { Id = Guid.NewGuid(), PaymentId = paymentId, Account = account, DebitAmount = amount, Type = type, CreatedAt = DateTime.UtcNow };

    public static LedgerEntry Credit(Guid paymentId, string account, decimal amount, LedgerEntryType type) =>
        new() { Id = Guid.NewGuid(), PaymentId = paymentId, Account = account, CreditAmount = amount, Type = type, CreatedAt = DateTime.UtcNow };
}
