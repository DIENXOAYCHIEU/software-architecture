using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Application;
using PaymentService.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentService.Infrastructure;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string EventName { get; set; }
    public required string Payload { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}

public sealed class ProcessedIntegrationMessage
{
    public required string MessageId { get; set; }
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string Exchange { get; init; } = "swiftride.events";
    public string Queue { get; init; } = "payment.events";
    public string DeadLetterExchange { get; init; } = "swiftride.events.dlx";
    public string DeadLetterQueue { get; init; } = "payment.events.dlq";
}

public sealed class RabbitMqConnection(RabbitMqOptions options) : IDisposable
{
    private readonly IConnection connection = new ConnectionFactory
    {
        HostName = options.Host,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password,
        DispatchConsumersAsync = true
    }.CreateConnection();

    public IModel CreateChannel() => connection.CreateModel();
    public void Dispose() => connection.Dispose();
}

public sealed class OutboxEventPublisher(PaymentDbContext db) : IEventPublisher
{
    public Task PublishAsync(string eventName, object payload, CancellationToken ct = default)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            EventName = eventName,
            Payload = JsonSerializer.Serialize(payload)
        });
        return Task.CompletedTask;
    }
}

public sealed class PaymentOutboxWorker(
    IServiceScopeFactory scopes,
    RabbitMqConnection rabbit,
    RabbitMqOptions options,
    ILogger<PaymentOutboxWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                var messages = await db.OutboxMessages
                    .Where(x => x.PublishedAtUtc == null)
                    .OrderBy(x => x.OccurredAtUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);
                using var channel = rabbit.CreateChannel();
                channel.ExchangeDeclare(options.Exchange, ExchangeType.Topic, durable: true);
                foreach (var message in messages)
                {
                    try
                    {
                        var body = JsonSerializer.Serialize(new
                        {
                            MessageId = message.Id,
                            message.EventName,
                            message.Payload
                        });
                        channel.BasicPublish(options.Exchange, message.EventName,
                            basicProperties: null,
                            body: System.Text.Encoding.UTF8.GetBytes(body));
                        message.PublishedAtUtc = DateTime.UtcNow;
                        message.Attempts++;
                    }
                    catch (Exception ex)
                    {
                        message.Attempts++;
                        message.LastError = ex.Message;
                        log.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                    }
                }
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Payment outbox iteration failed");
            }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}

public sealed class PaymentIntegrationConsumer(
    IServiceScopeFactory scopes,
    RabbitMqConnection rabbit,
    RabbitMqOptions options,
    ILogger<PaymentIntegrationConsumer> log) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = rabbit.CreateChannel();
        channel.ExchangeDeclare(options.Exchange, ExchangeType.Topic, durable: true);
        channel.ExchangeDeclare(options.DeadLetterExchange, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(options.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(options.DeadLetterQueue, options.DeadLetterExchange, "#");
        var queueArguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = options.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = options.DeadLetterQueue
        };
        channel.QueueDeclare(options.Queue, durable: true, exclusive: false, autoDelete: false, queueArguments);
        channel.QueueBind(options.Queue, options.Exchange, "RideCompleted");
        channel.QueueBind(options.Queue, options.Exchange, "TripCompensationRequested");
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, args) =>
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<IntegrationEnvelope>(
                    System.Text.Encoding.UTF8.GetString(args.Body.ToArray()))
                    ?? throw new InvalidOperationException("Invalid integration message.");
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                if (await db.ProcessedIntegrationMessages.AnyAsync(
                        x => x.MessageId == envelope.MessageId, stoppingToken))
                {
                    channel.BasicAck(args.DeliveryTag, false);
                    return;
                }
                var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventConsumer>();
                await handler.HandleAsync(envelope.EventName, envelope.Payload, stoppingToken);
                db.ProcessedIntegrationMessages.Add(new ProcessedIntegrationMessage
                {
                    MessageId = envelope.MessageId
                });
                await db.SaveChangesAsync(stoppingToken);
                channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Payment integration message failed");
                channel.BasicNack(args.DeliveryTag, false, requeue: false);
            }
        };
        channel.BasicConsume(options.Queue, autoAck: false, consumer);
        return Task.CompletedTask;
    }

    private sealed record IntegrationEnvelope(string MessageId, string EventName, string Payload);
}

public sealed class PaymentIntegrationHandler(
    PaymentAppService payments,
    PaymentDbContext db,
    IConfiguration configuration) : IIntegrationEventConsumer
{
    public async Task HandleAsync(string eventName, string payload, CancellationToken ct)
    {
        if (eventName == "RideCompleted")
        {
            var completed = JsonSerializer.Deserialize<RideCompleted>(payload)
                ?? throw new InvalidOperationException("Invalid RideCompleted payload.");
            var token = configuration["Payment:DemoPaymentToken"] ?? "tok_event_demo";
            await payments.CreateAsync(new CreatePaymentRequest(
                completed.TripId, completed.RiderId, completed.Amount,
                PaymentMethod.Card, token), $"trip:{completed.TripId}:payment", ct);
        }
        else if (eventName == "TripCompensationRequested")
        {
            var compensation = JsonSerializer.Deserialize<TripCompensationRequested>(payload)
                ?? throw new InvalidOperationException("Invalid compensation payload.");
            var payment = await db.Payments.FirstOrDefaultAsync(
                x => x.TripId == compensation.TripId &&
                     x.Status == PaymentStatus.Succeeded, ct);
            if (payment is not null)
                await payments.RefundAsync(payment.Id,
                    $"trip:{compensation.TripId}:compensation", ct);
        }
    }

    private sealed record RideCompleted(Guid TripId, Guid RiderId, decimal Amount);
    private sealed record TripCompensationRequested(Guid TripId, string Reason);
}
