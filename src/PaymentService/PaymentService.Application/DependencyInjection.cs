using Microsoft.Extensions.DependencyInjection;

namespace PaymentService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PaymentAppService>();
        return services;
    }
}
