namespace MatchingService.Application.DTOs;

public class MatchingResponse
{
    public Guid MatchingId { get; set; }

    public Guid TripId { get; set; }

    public Guid? DriverId { get; set; }

    public double PickupLatitude { get; set; }

    public double PickupLongitude { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? MatchedAt { get; set; }
}