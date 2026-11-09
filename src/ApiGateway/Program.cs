using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt");
var authority = jwtSettings["Authority"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    if (!string.IsNullOrWhiteSpace(authority))
    {
        options.Authority = authority;
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = string.IsNullOrWhiteSpace(authority),
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        NameClaimType = "name",
        IssuerSigningKey = string.IsNullOrWhiteSpace(authority)
            ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
            : null
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(transformContext =>
        {
            var user = transformContext.HttpContext.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? user.FindFirst("id")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    transformContext.ProxyRequest.Headers.Remove("X-User-Id");
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Id", userId);
                }
            }

            return ValueTask.CompletedTask;
        });
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapReverseProxy().RequireRateLimiting("api");
app.Run();
