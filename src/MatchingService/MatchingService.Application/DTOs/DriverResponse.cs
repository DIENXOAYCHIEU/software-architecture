namespace MatchingService.Application.DTOs;

public class DriverResponse
{
    public Guid DriverId { get; set; }

    public string Name { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Status { get; set; } = string.Empty;
}
