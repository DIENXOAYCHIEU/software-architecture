using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application;
using PaymentService.Domain;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, ReliabilityOptions options)
    {
        services.AddDbContext<PaymentDbContext>(o =>
            o.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentProcessorFactory, PaymentProcessorFactory>();
        services.AddSingleton<IEventPublisher, LoggingEventPublisher>();
        services.AddSingleton<IPaymentTokenProvider, InMemoryPaymentTokenProvider>();
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.AddSingleton(options);
        services.AddHostedService<PaymentRetryWorker>();
        return services;
    }

    public sealed class InMemoryPaymentTokenProvider : IPaymentTokenProvider
    {
        private readonly Dictionary<Guid, string> tokens = [];
        private readonly object gate = new();

        public Task StoreTokenAsync(Payment payment, string token, CancellationToken ct)
        {
            lock (gate) tokens[payment.Id] = token;
            return Task.CompletedTask;
        }

        public Task<string?> GetTokenAsync(Payment payment, CancellationToken ct)
        {
            string? token;
            lock (gate) tokens.TryGetValue(payment.Id, out token);
            return Task.FromResult(token);
        }
    }

    /// <summary>Apply pending EF Core migrations for the payment database.</summary>
    public static void InitializePaymentDatabase(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.Migrate();
    }
}
