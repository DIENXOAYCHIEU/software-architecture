using Microsoft.EntityFrameworkCore;

using TripService.Infrastructure.Persistence;
using TripService.Infrastructure.Persistence.Repositories;

using TripService.Application.Interfaces;
using TripService.Application.Services;

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

var app = builder.Build();

// app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
