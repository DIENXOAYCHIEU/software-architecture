namespace MatchingService.Application;

public sealed record MatchResult(
    bool Matched,
    string? DriverId,
    string? DriverName,
    double? DriverLatitude,
    double? DriverLongitude,
    double? DistanceMeters
);