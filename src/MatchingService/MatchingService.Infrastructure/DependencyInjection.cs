using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MatchingService.Application.Interfaces;
using MatchingService.Infrastructure.Persistence;
using MatchingService.Infrastructure.Persistence.Repositories;
using MatchingService.Infrastructure.Routing;

namespace MatchingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("MatchingServiceDb")
            ?? throw new InvalidOperationException(
                "Connection string 'MatchingServiceDb' is not configured.");

        services.AddDbContext<MatchingDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(
                    new Version(8, 4, 0))));
        services.AddHttpClient<
            IRoutingService,
            OpenRouteService>();

        // Repositories
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IMatchingRepository, MatchingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
