using MatchingService.Application.Interfaces;
using MatchingService.Application.Services;
using MatchingService.Infrastructure.Persistence;
using MatchingService.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection("MongoDbSettings"));

        services.AddSingleton<MongoDbContext>();

        services.AddScoped<IMatchingRepository,
            MatchingRepository>();

        services.AddScoped<IDriverRepository,
            DriverRepository>();

        services.AddScoped<IMatchingService,
            MatchingService.Application.Services.MatchingService>();

        services.AddScoped<IDriverService,
            DriverService>();

        return services;
    }
}