using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MatchingService.Application.DTOs;
using MatchingService.Application.Interfaces;

namespace MatchingService.Infrastructure.Integration;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string Exchange { get; init; } = "swiftride.events";
    public string Queue { get; init; } = "matching.events"; 
    public string DeadLetterExchange { get; init; } = "swiftride.events.dlx";
    public string DeadLetterQueue { get; init; } = "matching.events.dlq";

    public string PricingQueue { get; init; } = "pricing.events";

    public string PricingDeadLetterExchange { get; init; } = "swiftride.pricing.events.dlx";

    public string PricingDeadLetterQueue { get; init; } = "pricing.events.dlq";
}

// Cấu trúc Envelope bọc 2 lớp bắt buộc giống TripService
public sealed record Envelope(string MessageId, string EventName, string Payload);

// Contract dữ liệu nhận vào từ TripCreated của TripService
public sealed record TripCreatedPayload(
    Guid TripId, 
    double PickupLatitude, 
    double PickupLongitude, 
    double DropoffLatitude, 
    double DropoffLongitude);

// Contract dữ liệu phản hồi ngược về hệ thống
public sealed record DriverAssignedPayload(Guid TripId, Guid DriverId);

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

public sealed class TripCreatedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly RabbitMqConnection _rabbit;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<TripCreatedConsumer> _log;

    public TripCreatedConsumer(
        IServiceScopeFactory scopes, RabbitMqConnection rabbit, RabbitMqOptions options,
        ILogger<TripCreatedConsumer> log)
    {
        _scopes = scopes;
        _rabbit = rabbit;
        _options = options;
        _log = log;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = _rabbit.CreateChannel();
        
        channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true);
        channel.ExchangeDeclare(_options.DeadLetterExchange, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(_options.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(_options.DeadLetterQueue, _options.DeadLetterExchange, "#");

        var queueArguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _options.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = _options.DeadLetterQueue
        };
        
        channel.QueueDeclare(_options.Queue, durable: true, exclusive: false, autoDelete: false, queueArguments);
        channel.QueueBind(_options.Queue, _options.Exchange, "TripCreated");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, args) =>
        {
            try
            {
                var bodyString = System.Text.Encoding.UTF8.GetString(args.Body.ToArray());
                var envelope = JsonSerializer.Deserialize<Envelope>(bodyString)
                    ?? throw new InvalidOperationException("Invalid integration envelope.");

                if (envelope.EventName == "TripCreated")
                {
                    var tripData = JsonSerializer.Deserialize<TripCreatedPayload>(envelope.Payload)
                        ?? throw new InvalidOperationException("Invalid TripCreated payload.");

                    _log.LogInformation("[RabbitMQ] Nhận lệnh Matching tự động cho TripId: {TripId}", tripData.TripId);

                    using var scope = _scopes.CreateScope();
                    var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    // 1. Tự động hóa BƯỚC 1: Khởi tạo yêu cầu matching
                    var createRequest = new CreateMatchingRequest
                    {
                        TripId = tripData.TripId,
                        PickupLatitude = tripData.PickupLatitude,
                        PickupLongitude = tripData.PickupLongitude
                    };

                    var createdResponse = await matchingService.CreateAsync(createRequest, stoppingToken);

                    // 2. Tự động hóa BƯỚC 2: Kích hoạt thuật toán tìm kiếm tài xế gần nhất
                    var matchingResult = await matchingService.StartMatchingAsync(
                        createdResponse.MatchingId,
                        stoppingToken);

                    if (matchingResult.Status == "Failed")
                    {
                        _log.LogWarning(
                            "[Matching] Không tìm được tài xế cho TripId: {TripId}",
                            tripData.TripId);
                    }
                    else
                    {
                        _log.LogInformation(
                            "[Matching] Đã tìm và đề xuất tài xế cho TripId: {TripId}. Status: {Status}",
                            tripData.TripId,
                            matchingResult.Status);
                    }
                }

                channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[RabbitMQ] Lỗi xử lý sự kiện TripCreated");
                channel.BasicNack(args.DeliveryTag, false, false);
            }
        };

        channel.BasicConsume(_options.Queue, false, consumer);
        return Task.CompletedTask;
    }

    private async Task<Guid?> GetDriverFromResponseAsync(IMatchingService matchingService, Guid matchingId, CancellationToken ct)
    {
        // Hàm phụ hỗ trợ đọc lại bản ghi để lấy chính xác DriverId đang được gán
        var res = await matchingService.GetByIdAsync(matchingId, ct);
        // Thay thuộc tính khớp với MatchingResponse thực tế của bạn
        // return res?.DriverId; 
        return Guid.NewGuid();
    }

    private Task PublishDriverAssignedAsync(IModel channel, Guid tripId, Guid driverId)
    {
        var messageId = Guid.NewGuid().ToString();
        var innerPayload = JsonSerializer.Serialize(new DriverAssignedPayload(tripId, driverId));
        
        var envelopeBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            MessageId = messageId,
            EventName = "DriverAssigned",
            Payload = innerPayload
        }));

        channel.BasicPublish(_options.Exchange, "DriverAssigned", null, envelopeBytes);
        _log.LogInformation("[RabbitMQ] Đã bắn sự kiện DriverAssigned lên hệ thống cho TripId: {TripId}", tripId);
        return Task.CompletedTask;
    }
}

public static class IntegrationRegistration
{
    public static IServiceCollection AddMatchingInfrastructureIntegration(this IServiceCollection services, RabbitMqOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<RabbitMqConnection>();
        services.AddHostedService<TripCreatedConsumer>();
        services.AddHostedService<TripPricingConsumer>();
        services.AddSingleton<IIntegrationEventPublisher,RabbitMqIntegrationEventPublisher>();
        return services;
    }
}
