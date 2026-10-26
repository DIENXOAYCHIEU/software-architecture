using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;

namespace PaymentService.Infrastructure;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedIntegrationMessage> ProcessedIntegrationMessages => Set<ProcessedIntegrationMessage>();
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Method).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.RequestFingerprint).HasMaxLength(64).IsRequired();
            e.Property(x => x.RefundIdempotencyKey).HasMaxLength(100);
            e.Property(x => x.AttemptCount);
            e.Property(x => x.NextRetryAtUtc);
            e.Property(x => x.FailureReason).HasMaxLength(500);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.HasIndex(x => x.TripId);
            e.HasMany(x => x.LedgerEntries).WithOne().HasForeignKey(x => x.PaymentId);
        });

        b.Entity<LedgerEntry>(e =>
        {
            e.ToTable("ledger_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Account).HasMaxLength(100).IsRequired();
            e.Property(x => x.DebitAmount).HasPrecision(18, 2);
            e.Property(x => x.CreditAmount).HasPrecision(18, 2);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Payload).IsRequired();
            e.HasIndex(x => x.PublishedAtUtc);
        });
        b.Entity<ProcessedIntegrationMessage>(e =>
        {
            e.ToTable("processed_integration_messages");
            e.HasKey(x => x.MessageId);
        });
        b.Entity<AuditTrail>(e =>
        {
            e.ToTable("audit_trails");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.Details).HasMaxLength(1000);
            e.Property(x => x.CorrelationId).HasMaxLength(100);
            e.HasIndex(x => x.PaymentId);
            e.HasIndex(x => x.OccurredAtUtc);
        });
    }
}
