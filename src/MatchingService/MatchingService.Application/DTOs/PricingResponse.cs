namespace MatchingService.Application.DTOs;

public class PricingResponse
{
    public double DistanceKm { get; init; }

    public double DurationMinutes { get; init; }

    public decimal BasePrice { get; init; }

    public decimal SurgeMultiplier { get; init; }

    public decimal Toll { get; init; }

    public decimal PromotionMultiplier { get; init; }

    public decimal TaxRate { get; init; }

    public decimal FinalPrice { get; init; }

    public string Currency { get; init; } = "VND";
}