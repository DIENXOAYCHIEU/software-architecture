using Microsoft.Extensions.Logging.Abstractions;
using PaymentService.Application;
using PaymentService.Domain;
using Xunit;

namespace PaymentService.Tests;

public sealed class PaymentCoreTests
{
    [Fact]
    public async Task CreateAsync_ReplayingSameRequestDoesNotChargeTwice()
    {
        var repository = new InMemoryPaymentRepository();
        var processor = new TestPaymentProcessor();
        var service = CreateService(repository, processor);
        var request = CreateRequest();

        var first = await service.CreateAsync(request, "payment-key", CancellationToken.None);
        var replay = await service.CreateAsync(request, "payment-key", CancellationToken.None);

        Assert.True(first.Created);
        Assert.False(replay.Created);
        Assert.Equal(first.Payment.Id, replay.Payment.Id);
        Assert.Equal(1, processor.ChargeCalls);
    }

    [Fact]
    public async Task CreateAsync_SameKeyWithDifferentPayloadReturnsConflict()
    {
        var repository = new InMemoryPaymentRepository();
        var service = CreateService(repository, new TestPaymentProcessor());
        var request = CreateRequest();

        await service.CreateAsync(request, "payment-key", CancellationToken.None);

        var changedRequest = request with { Amount = request.Amount + 1 };
        var exception = await Assert.ThrowsAsync<PaymentException>(() =>
            service.CreateAsync(changedRequest, "payment-key", CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task RefundAsync_ReplayingSameKeyDoesNotRefundTwice()
    {
        var repository = new InMemoryPaymentRepository();
        var processor = new TestPaymentProcessor();
        var service = CreateService(repository, processor);
        var created = await service.CreateAsync(CreateRequest(), "payment-key", CancellationToken.None);

        var first = await service.RefundAsync(created.Payment.Id, "refund-key", CancellationToken.None);
        var replay = await service.RefundAsync(created.Payment.Id, "refund-key", CancellationToken.None);

        Assert.Equal(PaymentStatus.Refunded, first.Status);
        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(1, processor.RefundCalls);
        Assert.Equal(4, first.LedgerEntries.Count);
    }

    [Fact]
    public async Task RefundAsync_DifferentKeyAfterRefundReturnsConflict()
    {
        var repository = new InMemoryPaymentRepository();
        var service = CreateService(repository, new TestPaymentProcessor());
        var created = await service.CreateAsync(CreateRequest(), "payment-key", CancellationToken.None);

        await service.RefundAsync(created.Payment.Id, "refund-key", CancellationToken.None);

        var exception = await Assert.ThrowsAsync<PaymentException>(() =>
            service.RefundAsync(created.Payment.Id, "another-refund-key", CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public void SuccessfulPaymentAndRefundKeepLedgerBalanced()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), 85000m, "VND", PaymentMethod.Card, "key", "fingerprint");

        payment.MarkSucceeded();
        AssertLedgerBalanced(payment, LedgerEntryType.Charge);

        payment.Refund("refund-key");
        AssertLedgerBalanced(payment, LedgerEntryType.Refund);
        Assert.Equal(0m, payment.LedgerEntries.Sum(x => x.DebitAmount - x.CreditAmount));
    }

    [Fact]
    public void PaymentRejectsInvalidStateTransitions()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), 100m, "VND", PaymentMethod.Card, "key", "fingerprint");

        payment.MarkFailed("declined");
        Assert.Throws<InvalidOperationException>(payment.MarkSucceeded);
        Assert.Throws<InvalidOperationException>(() => payment.Refund("refund-key"));
    }

    private static PaymentAppService CreateService(
        InMemoryPaymentRepository repository,
        TestPaymentProcessor processor) =>
        new(
            repository,
            new TestProcessorFactory(processor),
            new TestEventPublisher(),
            NullLogger<PaymentAppService>.Instance);

    private static CreatePaymentRequest CreateRequest() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 85000m, PaymentMethod.Card, "tok_test");

    private static void AssertLedgerBalanced(Payment payment, LedgerEntryType type)
    {
        var entries = payment.LedgerEntries.Where(x => x.Type == type).ToList();
        Assert.Equal(2, entries.Count);
        Assert.Equal(entries.Sum(x => x.DebitAmount), entries.Sum(x => x.CreditAmount));
    }

    private sealed class InMemoryPaymentRepository : IPaymentRepository
    {
        private readonly List<Payment> payments = [];

        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(payments.SingleOrDefault(x => x.Id == id));

        public Task<Payment?> GetByIdempotencyKeyAsync(string key, CancellationToken ct) =>
            Task.FromResult(payments.SingleOrDefault(x => x.IdempotencyKey == key));

        public Task<bool> HasSucceededForTripAsync(Guid tripId, CancellationToken ct) =>
            Task.FromResult(payments.Any(x => x.TripId == tripId && x.Status == PaymentStatus.Succeeded));

        public Task<List<Payment>> GetByTripAsync(Guid tripId, CancellationToken ct) =>
            Task.FromResult(payments.Where(x => x.TripId == tripId).ToList());

        public Task<List<LedgerEntry>> GetLedgerAsync(Guid paymentId, CancellationToken ct) =>
            Task.FromResult(payments.Single(x => x.Id == paymentId).LedgerEntries.ToList());

        public Task AddAsync(Payment payment, CancellationToken ct)
        {
            if (payments.Any(x => x.IdempotencyKey == payment.IdempotencyKey))
                throw new DuplicateIdempotencyKeyException();
            payments.Add(payment);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class TestProcessorFactory(TestPaymentProcessor processor) : IPaymentProcessorFactory
    {
        public IPaymentProcessor Create(PaymentMethod method) => processor;
    }

    private sealed class TestPaymentProcessor : IPaymentProcessor
    {
        public int ChargeCalls { get; private set; }
        public int RefundCalls { get; private set; }

        public Task<GatewayResult> ChargeAsync(decimal amount, string currency, string token, CancellationToken ct)
        {
            ChargeCalls++;
            return Task.FromResult(new GatewayResult(true));
        }

        public Task<GatewayResult> RefundAsync(decimal amount, string currency, CancellationToken ct)
        {
            RefundCalls++;
            return Task.FromResult(new GatewayResult(true));
        }
    }

    private sealed class TestEventPublisher : IEventPublisher
    {
        public Task PublishAsync(string eventName, object payload, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
