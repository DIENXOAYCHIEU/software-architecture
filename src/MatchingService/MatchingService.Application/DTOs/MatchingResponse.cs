namespace MatchingService.Application.DTOs;

public class MatchingResponse
{
    public Guid MatchingId { get; init; }

    public Guid TripId { get; init; }

    public Guid? DriverId { get; init; }

    public double PickupLatitude { get; init; }

    public double PickupLongitude { get; init; }

    public string Status { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public int MaxAttempts { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? MatchedAt { get; init; }
}