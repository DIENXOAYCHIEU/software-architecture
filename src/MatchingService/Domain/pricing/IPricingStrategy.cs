using MatchingService.Application;

namespace MatchingService.Domain.Pricing;

public interface IPricingStrategy
{
    string PricingType { get; }

    decimal Calculate(
        QuoteRequest request,
        out decimal distanceFare,
        out decimal durationFare,
        out decimal subtotal,
        out decimal surgeMultiplier,
        out decimal discountAmount);
}