
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MatchingService.Application.DTOs;
using MatchingService.Application.Interfaces;

namespace MatchingService.Infrastructure.Integration;

public sealed record PriceCalculatedPayload(
    Guid TripId,
    double DistanceKm,
    double DurationMinutes,
    decimal BasePrice,
    decimal SurgeMultiplier,
    decimal Toll,
    decimal PromotionMultiplier,
    decimal TaxRate,
    decimal FinalPrice);

public sealed class TripPricingConsumer : BackgroundService
{
    private const string RoutingKey = "TripCreated";
    private const string ResultRoutingKey = "PriceCalculated";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnection _rabbit;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<TripPricingConsumer> _logger;

    public TripPricingConsumer(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnection rabbit,
        RabbitMqOptions options,
        ILogger<TripPricingConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbit = rabbit;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var channel = _rabbit.CreateChannel();

        try
        {
            // 1. Khai báo Exchange
            channel.ExchangeDeclare(
                _options.Exchange,
                ExchangeType.Topic,
                durable: true);

            // 2. Khai báo Pricing Dead Letter Exchange
            channel.ExchangeDeclare(
                _options.PricingDeadLetterExchange,
                ExchangeType.Topic,
                durable: true);

            // 3. Khai báo Pricing DLQ
            channel.QueueDeclare(
                _options.PricingDeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            channel.QueueBind(
                _options.PricingDeadLetterQueue,
                _options.PricingDeadLetterExchange,
                _options.PricingDeadLetterQueue);

            // 4. Khai báo Pricing Queue
            var queueArguments = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] =
                    _options.PricingDeadLetterExchange,

                ["x-dead-letter-routing-key"] =
                    _options.PricingDeadLetterQueue
            };

            channel.QueueDeclare(
                _options.PricingQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments);

            // 5. Binding TripCreated
            channel.QueueBind(
                _options.PricingQueue,
                _options.Exchange,
                RoutingKey);

            channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += async (_, args) =>
            {
                try
                {
                    var body = Encoding.UTF8.GetString(
                        args.Body.ToArray());

                    var envelope =
                        JsonSerializer.Deserialize<Envelope>(
                            body,
                            JsonOptions)
                        ?? throw new InvalidOperationException(
                            "Invalid event envelope.");

                    if (!string.Equals(
                        envelope.EventName,
                        RoutingKey,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        channel.BasicAck(args.DeliveryTag, false);
                        return;
                    }

                    var tripData =
                        JsonSerializer.Deserialize<TripCreatedPayload>(
                            envelope.Payload,
                            JsonOptions)
                        ?? throw new InvalidOperationException(
                            "Invalid TripCreated payload.");

                    _logger.LogInformation(
                        "[Pricing] Nhận TripCreated: {TripId}",
                        tripData.TripId);

                    // Mỗi message sử dụng một DI scope riêng
                    using var scope = _scopeFactory.CreateScope();

                    var pricingService =
                        scope.ServiceProvider.GetRequiredService<IPricingService>();

                    // 6. Tạo PricingRequest
                    var request = new PricingRequest
                    {
                        PickupLatitude = tripData.PickupLatitude,
                        PickupLongitude = tripData.PickupLongitude,

                        DestinationLatitude = tripData.DropoffLatitude,
                        DestinationLongitude = tripData.DropoffLongitude,

                        SurgeMultiplier = 1.0m,
                        Toll = 0m,
                        PromotionMultiplier = 1.0m,
                        TaxRate = 0.10m
                    };

                    // 7. Tính giá
                    var result = await pricingService.CalculateAsync(
                        request,
                        stoppingToken);

                    _logger.LogInformation(
                        "[Pricing] TripId: {TripId}, Distance: {Distance} km, Price: {Price}",
                        tripData.TripId,
                        result.DistanceKm,
                        result.FinalPrice);

                    // 8. Tạo PriceCalculated event
                    var payload = new PriceCalculatedPayload(
                        tripData.TripId,
                        result.DistanceKm,
                        result.DurationMinutes,
                        result.BasePrice,
                        result.SurgeMultiplier,
                        result.Toll,
                        result.PromotionMultiplier,
                        result.TaxRate,
                        result.FinalPrice);

                    var innerPayload =
                        JsonSerializer.Serialize(payload);

                    var outgoingEnvelope = new Envelope(
                        Guid.NewGuid().ToString(),
                        "PriceCalculated",
                        innerPayload);

                    var messageBytes = Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(outgoingEnvelope));

                    // 9. Publish kết quả tính giá
                    var properties = channel.CreateBasicProperties();

                    properties.Persistent = true;
                    properties.ContentType = "application/json";
                    properties.MessageId = outgoingEnvelope.MessageId;

                    channel.BasicPublish(
                        _options.Exchange,
                        ResultRoutingKey,
                        properties,
                        messageBytes);

                    _logger.LogInformation(
                        "[Pricing] Đã publish PriceCalculated cho TripId: {TripId}",
                        tripData.TripId);

                    // 10. ACK sau khi publish
                    channel.BasicAck(args.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[Pricing] Lỗi xử lý TripCreated.");

                    if (channel.IsOpen)
                    {
                        channel.BasicNack(
                            args.DeliveryTag,
                            multiple: false,
                            requeue: false);
                    }
                }
            };

            channel.BasicConsume(
                _options.PricingQueue,
                autoAck: false,
                consumer);

            _logger.LogInformation(
                "[Pricing] Consumer đang lắng nghe queue: {Queue}",
                _options.PricingQueue);

            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "[Pricing] Consumer đang dừng.");
        }
        finally
        {
            if (channel.IsOpen)
                channel.Close();

            channel.Dispose();
        }
    }
}
