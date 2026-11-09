namespace MatchingService.Application.DTOs;

public class CreateMatchingRequest
{
    public Guid TripId { get; set; }

    public double PickupLatitude { get; set; }

    public double PickupLongitude { get; set; }
}