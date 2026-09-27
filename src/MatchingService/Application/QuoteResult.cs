namespace MatchingService.Application;

public record QuoteResult(
    double DistanceKm,
    double DurationMinutes,
    string PricingType,
    decimal BaseFare,
    decimal DistanceFare,
    decimal DurationFare,
    decimal Subtotal,
    decimal SurgeMultiplier,
    decimal DiscountAmount,
    decimal TotalFare
);