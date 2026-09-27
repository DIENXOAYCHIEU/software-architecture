namespace MatchingService.Application;

public record QuoteRequest(
    double DistanceKm,
    double DurationMinutes,
    string PricingType,
    decimal BaseFare,
    decimal PricePerKm,
    decimal PricePerMinute,
    decimal SurgeMultiplier = 1.0m,
    decimal DiscountPercent = 0.0m
);