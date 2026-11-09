using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Application;

namespace PaymentService.Infrastructure;

public sealed class PaymentRetryWorker(
    IServiceScopeFactory scopeFactory,
    ReliabilityOptions options,
    ILogger<PaymentRetryWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(options.PendingWorkerIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RetryDuePaymentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Payment retry worker iteration failed");
            }
        }
    }

    private async Task RetryDuePaymentsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var service = scope.ServiceProvider.GetRequiredService<PaymentAppService>();
        var payments = await repository.GetPendingForRetryAsync(
            DateTime.UtcNow, options.PendingWorkerBatchSize, ct);

        foreach (var payment in payments)
        {
            try
            {
                await service.RetryPendingAsync(payment, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Retry failed for payment {PaymentId}", payment.Id);
            }
        }
    }
}

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string? Current { get; set; }
}
