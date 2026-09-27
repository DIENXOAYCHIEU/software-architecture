using MatchingService.Application;

namespace MatchingService.Domain.Pricing;

public class NormalPricingStrategy : IPricingStrategy
{
    public string PricingType => "normal";

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
        discountAmount = 0m;

        return subtotal;
    }
}