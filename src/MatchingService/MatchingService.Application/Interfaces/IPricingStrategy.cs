namespace MatchingService.Application.Interfaces;

public interface IPricingStrategy
{
    decimal CalculatePrice(
        double distanceKm,
        double durationMinutes,
        decimal surgeMultiplier,
        decimal toll,
        decimal promotionMultiplier,
        decimal taxRate);
}