using System.Text;
using System.Text.Json;
using MatchingService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MatchingService.Infrastructure.Integration;

public sealed class RabbitMqIntegrationEventPublisher
    : IIntegrationEventPublisher
{
    private readonly RabbitMqConnection _rabbit;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqIntegrationEventPublisher> _log;

    public RabbitMqIntegrationEventPublisher(
        RabbitMqConnection rabbit,
        RabbitMqOptions options,
        ILogger<RabbitMqIntegrationEventPublisher> log)
    {
        _rabbit = rabbit;
        _options = options;
        _log = log;
    }

    public Task PublishDriverAssignedAsync(
        Guid tripId,
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var channel = _rabbit.CreateChannel();

        channel.ExchangeDeclare(
            exchange: _options.Exchange,
            type: RabbitMQ.Client.ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null);

        var payload = JsonSerializer.Serialize(
            new DriverAssignedPayload(tripId, driverId));

        var envelope = new Envelope(
            Guid.NewGuid().ToString(),
            "DriverAssigned",
            payload);

        var message = JsonSerializer.Serialize(envelope);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: _options.Exchange,
            routingKey: "DriverAssigned",
            mandatory: false,
            basicProperties: properties,
            body: body);

        _log.LogInformation(
            "[RabbitMQ] Đã publish DriverAssigned. TripId: {TripId}, DriverId: {DriverId}",
            tripId,
            driverId);

        return Task.CompletedTask;
    }
}