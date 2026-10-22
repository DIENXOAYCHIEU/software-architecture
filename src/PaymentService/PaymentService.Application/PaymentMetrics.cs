using System.Diagnostics.Metrics;

namespace PaymentService.Application;

public sealed class PaymentMetrics
{
    public const string MeterName = "SwiftRide.PaymentService";
    private readonly Counter<long> paymentsCreated;
    private readonly Counter<long> paymentsRefunded;
    private readonly Counter<long> gatewayFailures;

    public PaymentMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        paymentsCreated = meter.CreateCounter<long>("payment.created");
        paymentsRefunded = meter.CreateCounter<long>("payment.refunded");
        gatewayFailures = meter.CreateCounter<long>("payment.gateway.failures");
    }

    public void PaymentCreated(string status) =>
        paymentsCreated.Add(1, new KeyValuePair<string, object?>("status", status));

    public void PaymentRefunded() => paymentsRefunded.Add(1);

    public void GatewayFailure(string kind) =>
        gatewayFailures.Add(1, new KeyValuePair<string, object?>("kind", kind));
}
