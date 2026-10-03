namespace MatchingService.Application.DTOs;

public class RouteResult
{
    public double DistanceKm { get; init; }

    public double DurationMinutes { get; init; }

    public List<RouteCoordinate> Coordinates { get; init; } = [];
}

public class RouteCoordinate
{
    public double Longitude { get; init; }

    public double Latitude { get; init; }
}