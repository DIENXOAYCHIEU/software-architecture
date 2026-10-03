namespace MatchingService.Application.DTOs;

public class PricingRequest
{
    public double PickupLatitude { get; set; }

    public double PickupLongitude { get; set; }

    public double DestinationLatitude { get; set; }

    public double DestinationLongitude { get; set; }

    public decimal SurgeMultiplier { get; set; } = 1.0m;

    public decimal Toll { get; set; } = 0m;

    public decimal PromotionMultiplier { get; set; } = 1.0m;

    public decimal TaxRate { get; set; } = 0.10m;
}