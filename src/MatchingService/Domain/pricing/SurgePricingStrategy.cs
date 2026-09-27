using MatchingService.Application;

namespace MatchingService.Domain.Pricing;

public class SurgePricingStrategy : IPricingStrategy
{
    public string PricingType => "surge";

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

        surgeMultiplier =
            request.SurgeMultiplier <= 0
                ? 1.0m
                : request.SurgeMultiplier;

        discountAmount = 0m;

        return subtotal * surgeMultiplier;
    }
}