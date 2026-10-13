namespace PaymentService.Application;

public sealed class ReliabilityOptions
{
    public int MaxGatewayAttempts { get; init; } = 3;
    public int MaxTotalAttempts { get; init; } = 5;
    public int GatewayTimeoutSeconds { get; init; } = 5;
    public int RetryBaseDelayMilliseconds { get; init; } = 100;
    public int RetryMaxDelayMilliseconds { get; init; } = 2000;
    public int PendingWorkerIntervalSeconds { get; init; } = 10;
    public int PendingWorkerBatchSize { get; init; } = 20;
}
