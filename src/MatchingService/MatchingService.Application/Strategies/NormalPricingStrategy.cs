using MatchingService.Application.Interfaces;

namespace MatchingService.Application.Strategies;

public class NormalPricingStrategy : IPricingStrategy
{
    private const decimal CostPerKm = 10000m;

    private const decimal CostPerMinute = 2000m;

    public decimal CalculatePrice(
        double distanceKm,
        double durationMinutes,
        decimal surgeMultiplier,
        decimal toll,
        decimal promotionMultiplier,
        decimal taxRate)
    {
        if (distanceKm < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distanceKm));
        }

        if (durationMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationMinutes));
        }

        if (surgeMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(surgeMultiplier));
        }

        if (promotionMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(promotionMultiplier));
        }

        if (taxRate < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taxRate));
        }

        var basePrice =
            (CostPerKm * (decimal)distanceKm)
            +
            (CostPerMinute * (decimal)durationMinutes);

        var afterSurge =
            basePrice * surgeMultiplier;

        var afterToll =
            afterSurge + toll;

        var afterPromotion =
            afterToll * promotionMultiplier;

        var finalPrice =
            afterPromotion * (1m + taxRate);

        return decimal.Round(
            finalPrice,
            0,
            MidpointRounding.AwayFromZero);
    }
}