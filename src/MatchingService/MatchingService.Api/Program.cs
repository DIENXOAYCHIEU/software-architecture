using Microsoft.EntityFrameworkCore;
using MatchingService.Application.Interfaces;
using MatchingService.Application.Services;
using MatchingService.Infrastructure;
using MatchingService.Infrastructure.Persistence;
using MatchingService.Application.Strategies;
using MatchingService.Infrastructure.Integration;

using MatchingAppService = MatchingService.Application.Services.MatchingService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Application Services
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<IMatchingService, MatchingAppService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IPricingStrategy,NormalPricingStrategy>();

// Infrastructure
builder.Services.AddInfrastructureServices(
    builder.Configuration);

var rabbitOptions =
    builder.Configuration.GetSection("RabbitMq")
        .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

builder.Services.AddMatchingInfrastructureIntegration(rabbitOptions);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (builder.Configuration.GetValue<bool>("UseHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MatchingDbContext>();
        Console.WriteLine("--> [Docker] Đang quét file thiết kế và tạo bảng vào MySQL...");
        await context.Database.MigrateAsync();
        Console.WriteLine("--> [Docker] Đã tạo các bảng vào DB thành công!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "--> [Docker LỖI] Không thể tạo bảng vào DB.");
    }
}

app.Run();
