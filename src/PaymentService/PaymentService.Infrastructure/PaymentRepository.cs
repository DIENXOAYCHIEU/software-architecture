using Microsoft.EntityFrameworkCore;
using PaymentService.Application;
using PaymentService.Domain;

namespace PaymentService.Infrastructure;

public class PaymentRepository(PaymentDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Payment?> GetByIdempotencyKeyAsync(string key, CancellationToken ct) =>
        db.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == key, ct);

    public Task<bool> HasSucceededForTripAsync(Guid tripId, CancellationToken ct) =>
        db.Payments.AnyAsync(p => p.TripId == tripId && p.Status == PaymentStatus.Succeeded, ct);

    public Task<List<Payment>> GetByTripAsync(Guid tripId, CancellationToken ct) =>
        db.Payments.AsNoTracking().Where(p => p.TripId == tripId)
            .OrderByDescending(p => p.CreatedAt).ToListAsync(ct);

    public Task<List<LedgerEntry>> GetLedgerAsync(Guid paymentId, CancellationToken ct) =>
        db.LedgerEntries.AsNoTracking().Where(e => e.PaymentId == paymentId)
            .OrderBy(e => e.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(Payment payment, CancellationToken ct)
    {
        db.Payments.Add(payment);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var duplicated = await db.Payments.AnyAsync(p => p.IdempotencyKey == payment.IdempotencyKey, ct);
            if (duplicated) throw new DuplicateIdempotencyKeyException();
            throw;
        }
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
