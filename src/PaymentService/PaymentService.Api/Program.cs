using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application;
using PaymentService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration.GetConnectionString("PaymentServiceDb")!);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Services.InitializePaymentDatabase();

app.UseSwagger();
app.UseSwaggerUI();

// Middleware map lỗi nghiệp vụ -> HTTP status
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (PaymentException ex)
    {
        ctx.Response.StatusCode = ex.StatusCode;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        ctx.Response.StatusCode = 409;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "payment" }));

var g = app.MapGroup("/payments").WithTags("Payments");

// POST /payments — bắt buộc header Idempotency-Key
g.MapPost("/", async ([FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                      CreatePaymentRequest req, PaymentAppService svc, CancellationToken ct) =>
{
    var (payment, created) = await svc.CreateAsync(req, idempotencyKey, ct);
    var body = PaymentResponse.From(payment);
    return created ? Results.Created($"/payments/{payment.Id}", body) : Results.Ok(body);
});

g.MapGet("/{id:guid}", async (Guid id, PaymentAppService svc, CancellationToken ct) =>
    PaymentResponse.From(await svc.GetAsync(id, ct)));

g.MapGet("/by-trip/{tripId:guid}", async (Guid tripId, PaymentAppService svc, CancellationToken ct) =>
    (await svc.GetByTripAsync(tripId, ct)).Select(PaymentResponse.From));

g.MapGet("/{id:guid}/ledger", async (Guid id, PaymentAppService svc, CancellationToken ct) =>
    await svc.GetLedgerAsync(id, ct));

g.MapPost("/{id:guid}/refund", async (Guid id, PaymentAppService svc, CancellationToken ct) =>
    PaymentResponse.From(await svc.RefundAsync(id, ct)));

app.Run();
