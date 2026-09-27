using MatchingService.Api;
using MatchingService.Application;
using MatchingService.Infrastructure;
using MongoDB.Driver;
using MatchingService.Domain.Pricing;

var builder = WebApplication.CreateBuilder(args);

var mongoConnectionString =
    builder.Configuration["MongoDb:ConnectionString"]
    ?? throw new InvalidOperationException(
        "MongoDb:ConnectionString is missing.");

var mongoDatabaseName =
    builder.Configuration["MongoDb:DatabaseName"]
    ?? throw new InvalidOperationException(
        "MongoDb:DatabaseName is missing.");

builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(mongoConnectionString)
);

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();

    return client.GetDatabase(mongoDatabaseName);
});

builder.Services.AddSingleton<DriverRepository>();

builder.Services.AddScoped<
    IMatchingService,
    global::MatchingService.Application.MatchingService
>();

builder.Services.AddSingleton<IPricingStrategy, NormalPricingStrategy>();
builder.Services.AddSingleton<IPricingStrategy, SurgePricingStrategy>();
builder.Services.AddSingleton<IPricingStrategy, PromotionPricingStrategy>();

builder.Services.AddScoped<IPricingService, PricingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var repository =
        scope.ServiceProvider.GetRequiredService<DriverRepository>();

    await repository.EnsureIndexesAsync();
}

app.MapMatchingEndpoints();

app.Run();