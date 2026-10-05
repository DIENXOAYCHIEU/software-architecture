using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Persistence.Repositories;

using AuthService.Application.Interfaces;
using AuthService.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("AuthServiceDb")
    ?? throw new InvalidOperationException("Connection string 'AuthServiceDb' not found");

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySql(
        connectionString,
        Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect(connectionString)
    ));

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService.Application.Services.AuthService>();

var app = builder.Build();

app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
