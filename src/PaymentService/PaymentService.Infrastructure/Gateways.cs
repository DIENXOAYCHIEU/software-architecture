using Microsoft.Extensions.Logging;
using PaymentService.Application;
using PaymentService.Domain;

namespace PaymentService.Infrastructure;

// Bản giả lập cổng thanh toán: token bắt đầu bằng "tok_fail" => thất bại, còn lại => thành công.
public abstract class FakeProcessorBase : IPaymentProcessor
{
    public async Task<GatewayResult> ChargeAsync(decimal amount, string currency, string token, CancellationToken ct)
    {
        await Task.Delay(100, ct);
        return token.StartsWith("tok_fail", StringComparison.OrdinalIgnoreCase)
            ? new GatewayResult(false, "Thẻ/ví bị từ chối (giả lập).")
            : new GatewayResult(true);
    }

    public async Task<GatewayResult> RefundAsync(decimal amount, string currency, CancellationToken ct)
    {
        await Task.Delay(100, ct);
        return new GatewayResult(true);
    }
}

public sealed class CardProcessor : FakeProcessorBase { }
public sealed class WalletProcessor : FakeProcessorBase { }

public sealed class PaymentProcessorFactory : IPaymentProcessorFactory
{
    public IPaymentProcessor Create(PaymentMethod method) => method switch
    {
        PaymentMethod.Card => new CardProcessor(),
        PaymentMethod.Wallet => new WalletProcessor(),
        _ => throw new PaymentException(400, "Phương thức thanh toán không hỗ trợ.")
    };
}

public sealed class LoggingEventPublisher(ILogger<LoggingEventPublisher> log) : IEventPublisher
{
    public Task PublishAsync(string eventName, object payload, CancellationToken ct = default)
    {
        log.LogInformation("EVENT {Event}: {@Payload}", eventName, payload);
        return Task.CompletedTask;
    }
}
