using MatchingService.Application;

namespace MatchingService.Api;

public static class MatchingEndpoints
{
    public static void MapMatchingEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () =>
        {
            return Results.Ok(new
            {
                service = "MatchingService",
                status = "ok"
            });
        });

        app.MapPost(
            "/match",
            async (
                MatchRequest request,
                IMatchingService matchingService,
                CancellationToken cancellationToken) =>
            {
                var result = await matchingService.MatchAsync(
                    request,
                    cancellationToken);

                return Results.Ok(
                    result ?? new MatchResult(
                        Matched: false,
                        DriverId: null,
                        DriverName: null,
                        DriverLatitude: null,
                        DriverLongitude: null,
                        DistanceMeters: null
                    )
                );
            });
        
        app.MapPost(
            "/quote",
            (
                QuoteRequest request,
                IPricingService pricingService) =>
            {
                try
                {
                    var result = pricingService.CalculateQuote(request);

                    return Results.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new
                    {
                        error = ex.Message
                    });
                }
            });
    }
}