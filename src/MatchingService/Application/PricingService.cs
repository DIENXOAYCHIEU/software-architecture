using MatchingService.Domain.Pricing;

namespace MatchingService.Application;

public class PricingService : IPricingService
{
    private readonly Dictionary<string, IPricingStrategy> _strategies;

    public PricingService(IEnumerable<IPricingStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(
            x => x.PricingType,
            StringComparer.OrdinalIgnoreCase);
    }

    public QuoteResult CalculateQuote(QuoteRequest request)
    {
        if (request.DistanceKm < 0)
            throw new ArgumentException("DistanceKm cannot be negative.");

        if (request.DurationMinutes < 0)
            throw new ArgumentException(
                "DurationMinutes cannot be negative.");

        if (string.IsNullOrWhiteSpace(request.PricingType))
            throw new ArgumentException(
                "PricingType is required.");

        if (!_strategies.TryGetValue(
                request.PricingType,
                out var strategy))
        {
            throw new ArgumentException(
                $"Unsupported pricing type: {request.PricingType}");
        }

        var totalFare = strategy.Calculate(
            request,
            out var distanceFare,
            out var durationFare,
            out var subtotal,
            out var surgeMultiplier,
            out var discountAmount);

        return new QuoteResult(
            DistanceKm: request.DistanceKm,
            DurationMinutes: request.DurationMinutes,
            PricingType: strategy.PricingType,
            BaseFare: request.BaseFare,
            DistanceFare: distanceFare,
            DurationFare: durationFare,
            Subtotal: subtotal,
            SurgeMultiplier: surgeMultiplier,
            DiscountAmount: discountAmount,
            TotalFare: Math.Round(totalFare, 0)
        );
    }
}