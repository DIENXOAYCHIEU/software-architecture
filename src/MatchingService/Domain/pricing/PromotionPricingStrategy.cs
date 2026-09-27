using MatchingService.Application;

namespace MatchingService.Domain.Pricing;

public class PromotionPricingStrategy : IPricingStrategy
{
    public string PricingType => "promotion";

    public decimal Calculate(
        QuoteRequest request,
        out decimal distanceFare,
        out decimal durationFare,
        out decimal subtotal,
        out decimal surgeMultiplier,
        out decimal discountAmount)
    {
        distanceFare =
            (decimal)request.DistanceKm * request.PricePerKm;

        durationFare =
            (decimal)request.DurationMinutes * request.PricePerMinute;

        subtotal =
            request.BaseFare +
            distanceFare +
            durationFare;

        surgeMultiplier = 1.0m;

        var discountPercent =
            Math.Clamp(request.DiscountPercent, 0m, 100m);

        discountAmount =
            subtotal * discountPercent / 100m;

        return subtotal - discountAmount;
    }
}