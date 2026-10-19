using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TripService.Application.Interfaces;
using TripService.Domain.Enums;
using TripService.Infrastructure.Persistence;

namespace TripService.Infrastructure.Integration;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string Exchange { get; init; } = "swiftride.events";
    public string Queue { get; init; } = "trip.events";
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string EventName { get; set; }
    public required string Payload { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
}

public sealed class ProcessedIntegrationMessage
{
    public required string MessageId { get; set; }
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class RabbitMqConnection(RabbitMqOptions options) : IDisposable
{
    private readonly IConnection connection = new ConnectionFactory
    {
        HostName = options.Host, Port = options.Port,
        UserName = options.UserName, Password = options.Password,
        DispatchConsumersAsync = true
    }.CreateConnection();

    public IModel CreateChannel() => connection.CreateModel();
    public void Dispose() => connection.Dispose();
}

public sealed class TripEventPublisher(TripDbContext db) : ITripEventPublisher
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

public sealed class TripOutboxWorker(
    IServiceScopeFactory scopes, RabbitMqConnection rabbit, RabbitMqOptions options,
    ILogger<TripOutboxWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TripDbContext>();
                var messages = await db.OutboxMessages.Where(x => x.PublishedAtUtc == null)
                    .OrderBy(x => x.OccurredAtUtc).Take(20).ToListAsync(stoppingToken);
                using var channel = rabbit.CreateChannel();
                channel.ExchangeDeclare(options.Exchange, ExchangeType.Topic, durable: true);
                foreach (var message in messages)
                {
                    var body = JsonSerializer.Serialize(new
                    {
                        MessageId = message.Id,
                        message.EventName,
                        message.Payload
                    });
                    channel.BasicPublish(options.Exchange, message.EventName, null,
                        System.Text.Encoding.UTF8.GetBytes(body));
                    message.PublishedAtUtc = DateTime.UtcNow;
                }
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) { log.LogError(ex, "Trip outbox iteration failed"); }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}

public sealed class TripPaymentConsumer(
    IServiceScopeFactory scopes, RabbitMqConnection rabbit, RabbitMqOptions options,
    ILogger<TripPaymentConsumer> log) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = rabbit.CreateChannel();
        channel.ExchangeDeclare(options.Exchange, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(options.Queue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(options.Queue, options.Exchange, "PaymentSettled");
        channel.QueueBind(options.Queue, options.Exchange, "PaymentFailed");
        channel.QueueBind(options.Queue, options.Exchange, "PaymentRefunded");
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, args) =>
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<Envelope>(
                    System.Text.Encoding.UTF8.GetString(args.Body.ToArray()))
                    ?? throw new InvalidOperationException("Invalid payment event.");
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TripDbContext>();
                if (await db.ProcessedIntegrationMessages.AnyAsync(
                        x => x.MessageId == envelope.MessageId, stoppingToken))
                {
                    channel.BasicAck(args.DeliveryTag, false);
                    return;
                }
                var payment = JsonSerializer.Deserialize<PaymentEvent>(envelope.Payload)
                    ?? throw new InvalidOperationException("Invalid payment payload.");
                var trip = await db.Trips.FirstOrDefaultAsync(
                    x => x.Id == payment.TripId, stoppingToken)
                    ?? throw new InvalidOperationException("Trip for payment event was not found.");
                if (envelope.EventName == "PaymentSettled") trip.MarkPaid();
                else if (envelope.EventName == "PaymentFailed") trip.MarkPaymentPending();
                else trip.Cancel();
                db.ProcessedIntegrationMessages.Add(new ProcessedIntegrationMessage
                {
                    MessageId = envelope.MessageId
                });
                await db.SaveChangesAsync(stoppingToken);
                channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Trip payment event failed");
                channel.BasicNack(args.DeliveryTag, false, true);
            }
        };
        channel.BasicConsume(options.Queue, false, consumer);
        return Task.CompletedTask;
    }

    private sealed record Envelope(string MessageId, string EventName, string Payload);
    private sealed record PaymentEvent(Guid PaymentId, Guid TripId, decimal Amount);
}

public static class IntegrationRegistration
{
    public static IServiceCollection AddInfrastructureIntegration(
        this IServiceCollection services, RabbitMqOptions options)
    {
        services.AddScoped<ITripEventPublisher, TripEventPublisher>();
        services.AddSingleton(options);
        services.AddSingleton<RabbitMqConnection>();
        services.AddHostedService<TripOutboxWorker>();
        services.AddHostedService<TripPaymentConsumer>();
        return services;
    }
}
