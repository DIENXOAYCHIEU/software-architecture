using MatchingService.Application.Interfaces;
using MatchingService.Application.Services;
using MatchingService.Infrastructure;
using MatchingService.Application.Strategies;


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

app.Run();
