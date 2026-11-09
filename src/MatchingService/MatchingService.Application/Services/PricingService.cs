using MatchingService.Application.DTOs;
using MatchingService.Application.Interfaces;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Application.Services;

public class PricingService : IPricingService
{
    private readonly IRoutingService _routingService;
    private readonly IPricingStrategy _pricingStrategy;

    public PricingService(
        IRoutingService routingService,
        IPricingStrategy pricingStrategy)
    {
        _routingService = routingService;
        _pricingStrategy = pricingStrategy;
    }

    public async Task<PricingResponse> CalculateAsync(
        PricingRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var pickup = new Location(
            request.PickupLatitude,
            request.PickupLongitude);

        var destination = new Location(
            request.DestinationLatitude,
            request.DestinationLongitude);

        var route =
            await _routingService.CalculateRouteAsync(
                pickup,
                destination,
                cancellationToken);

        var finalPrice =
            _pricingStrategy.CalculatePrice(
                route.DistanceKm,
                route.DurationMinutes,
                request.SurgeMultiplier,
                request.Toll,
                request.PromotionMultiplier,
                request.TaxRate);

        var basePrice =
            (
                10000m * (decimal)route.DistanceKm
            )
            +
            (
                2000m * (decimal)route.DurationMinutes
            );

        return new PricingResponse
        {
            DistanceKm = route.DistanceKm,
            DurationMinutes = route.DurationMinutes,
            BasePrice = decimal.Round(
                basePrice,
                0,
                MidpointRounding.AwayFromZero),

            SurgeMultiplier =
                request.SurgeMultiplier,

            Toll =
                request.Toll,

            PromotionMultiplier =
                request.PromotionMultiplier,

            TaxRate =
                request.TaxRate,

            FinalPrice =
                finalPrice
        };
    }

    private static void ValidateRequest(
        PricingRequest request)
    {
        ValidateLatitude(
            request.PickupLatitude,
            nameof(request.PickupLatitude));

        ValidateLongitude(
            request.PickupLongitude,
            nameof(request.PickupLongitude));

        ValidateLatitude(
            request.DestinationLatitude,
            nameof(request.DestinationLatitude));

        ValidateLongitude(
            request.DestinationLongitude,
            nameof(request.DestinationLongitude));

        if (request.SurgeMultiplier <= 0)
        {
            throw new ArgumentException(
                "SurgeMultiplier must be greater than 0.");
        }

        if (request.Toll < 0)
        {
            throw new ArgumentException(
                "Toll cannot be negative.");
        }

        if (request.PromotionMultiplier <= 0)
        {
            throw new ArgumentException(
                "PromotionMultiplier must be greater than 0.");
        }

        if (request.TaxRate < 0)
        {
            throw new ArgumentException(
                "TaxRate cannot be negative.");
        }
    }

    private static void ValidateLatitude(
        double value,
        string fieldName)
    {
        if (value < -90 || value > 90)
        {
            throw new ArgumentException(
                $"{fieldName} must be between -90 and 90.");
        }
    }

    private static void ValidateLongitude(
        double value,
        string fieldName)
    {
        if (value < -180 || value > 180)
        {
            throw new ArgumentException(
                $"{fieldName} must be between -180 and 180.");
        }
    }
}