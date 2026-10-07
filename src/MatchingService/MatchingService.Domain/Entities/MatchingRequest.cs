using MatchingService.Domain.Enums;
using MatchingService.Domain.Exceptions;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Domain.Entities;

public class MatchingRequest
{
    public Guid Id { get; private set; }

    public Guid TripId { get; private set; }

    public Guid? DriverId { get; private set; }

    public Location PickupLocation { get; private set; } = null!;

    public MatchingStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? MatchedAt { get; private set; }

    public int AttemptCount { get; private set; }

    public int MaxAttempts { get; private set; } = 3;
    private MatchingRequest()
    {
    }

    public MatchingRequest(
        Guid tripId,
        Location pickupLocation)
    {
        if (tripId == Guid.Empty)
        {
            throw new InvalidMatchingException(
                "TripId cannot be empty.");
        }

        if (pickupLocation == null)
        {
            throw new InvalidMatchingException(
                "Pickup location is required.");
        }

        Id = Guid.NewGuid();

        TripId = tripId;

        PickupLocation = pickupLocation;

        Status = MatchingStatus.Pending;

        CreatedAt = DateTime.UtcNow;
    }

    public void StartSearching()
    {
        if (Status != MatchingStatus.Pending)
        {
            throw new InvalidMatchingException(
                "Matching request must be pending before searching.");
        }

        Status = MatchingStatus.Searching;
    }

    public void AssignDriver(Guid driverId)
    {
        if (driverId == Guid.Empty)
        {
            throw new InvalidMatchingException(
                "DriverId cannot be empty.");
        }

        if (Status != MatchingStatus.Searching)
        {
            throw new InvalidMatchingException(
                "Matching request must be searching before assigning driver.");
        }

        DriverId = driverId;

        Status = MatchingStatus.Matched;

        MatchedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        if (Status != MatchingStatus.Searching)
        {
            throw new InvalidMatchingException(
                "Only a searching request can be marked as failed.");
        }

        Status = MatchingStatus.Failed;
    }

    public void Cancel()
    {
        if (Status is not MatchingStatus.Pending and not MatchingStatus.Searching)
        {
            throw new InvalidMatchingException(
                "Only a pending or searching request can be cancelled.");
        }

        Status = MatchingStatus.Cancelled;
    }

    public void OfferDriver(Guid driverId)
    {
        DriverId = driverId;
    }
    public void IncrementAttempt()
    {
        AttemptCount++;
    }

    public void ClearDriver()
    {
        DriverId = null;
    }
}
