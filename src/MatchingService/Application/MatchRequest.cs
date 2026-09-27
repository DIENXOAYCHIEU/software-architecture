namespace MatchingService.Application;

public sealed record MatchRequest(
    double PickupLatitude,
    double PickupLongitude,
    double MaxDistanceMeters
);