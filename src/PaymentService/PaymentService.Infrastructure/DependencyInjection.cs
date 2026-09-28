using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PaymentDbContext>(o =>
            o.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentProcessorFactory, PaymentProcessorFactory>();
        services.AddSingleton<IEventPublisher, LoggingEventPublisher>();
        return services;
    }

    /// <summary>Tự tạo database + bảng (đồ án sơ bộ; sau này dùng EF Migrations).</summary>
    public static void InitializePaymentDatabase(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.EnsureCreated();
    }
}
