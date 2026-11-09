using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application;
using PaymentService.Domain;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, ReliabilityOptions options,
        RabbitMqOptions rabbitOptions)
    {
        services.AddDbContext<PaymentDbContext>(o =>
            o.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentProcessorFactory, PaymentProcessorFactory>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddScoped<IAuditTrailWriter, AuditTrailWriter>();
        services.AddSingleton<PaymentMetrics>();
        services.AddScoped<IPaymentTokenProvider, DatabasePaymentTokenProvider>();
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.AddSingleton(options);
        services.AddHostedService<PaymentRetryWorker>();
        services.AddSingleton(rabbitOptions);
        services.AddSingleton<RabbitMqConnection>();
        services.AddHostedService<PaymentOutboxWorker>();
        services.AddHostedService<PaymentIntegrationConsumer>();
        services.AddScoped<IIntegrationEventConsumer, PaymentIntegrationHandler>();
        return services;
    }

    public sealed class DatabasePaymentTokenProvider : IPaymentTokenProvider
    {
        public Task StoreTokenAsync(Payment payment, string token, CancellationToken ct)
        {
            payment.SetPaymentToken(token);
            return Task.CompletedTask;
        }

        public Task<string?> GetTokenAsync(Payment payment, CancellationToken ct) =>
            Task.FromResult(payment.PaymentToken);
    }

    /// <summary>Apply pending EF Core migrations for the payment database.</summary>
    public static void InitializePaymentDatabase(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.Migrate();
    }
}
