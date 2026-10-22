using Microsoft.EntityFrameworkCore;

using TripService.Infrastructure.Persistence;
using TripService.Infrastructure.Persistence.Repositories;

using TripService.Application.Interfaces;
using TripService.Application.Services;
using TripService.Infrastructure.Integration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("TripServiceDb")
    ?? throw new InvalidOperationException("Connection string 'TripServiceDb' not found");

builder.Services.AddDbContext<TripDbContext>(options =>
    options.UseMySql(
        connectionString,
        Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect(connectionString)
    ));

builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITripService, TripService.Application.Services.TripService>();
builder.Services.AddInfrastructureIntegration(builder.Configuration.GetSection("RabbitMq").Get<TripService.Infrastructure.Integration.RabbitMqOptions>()
    ?? new TripService.Infrastructure.Integration.RabbitMqOptions());

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TripDbContext>();
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
