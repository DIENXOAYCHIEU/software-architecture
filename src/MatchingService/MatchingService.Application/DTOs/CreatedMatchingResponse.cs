namespace MatchingService.Application.DTOs;

public class CreatedMatchingResponse
{
    public Guid MatchingId { get; set; }

    public Guid TripId { get; set; }

    public string Status { get; set; } = string.Empty;
}