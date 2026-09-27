using MatchingService.Domain.Enums;
using MatchingService.Domain.Exceptions;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Domain.Entities;

public class Driver
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Location CurrentLocation { get; private set; } = null!;

    public DriverStatus Status { get; private set; }

    private Driver()
    {
    }

    public Driver(
        string name,
        Location currentLocation)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidMatchingException(
                "Driver name is required.");
        }

        Id = Guid.NewGuid();

        Name = name.Trim();

        CurrentLocation = currentLocation
            ?? throw new InvalidMatchingException(
                "Driver location is required.");

        Status = DriverStatus.Available;
    }

    public void MarkBusy()
    {
        if (Status != DriverStatus.Available)
        {
            throw new InvalidMatchingException(
                "Driver is not available.");
        }

        Status = DriverStatus.Busy;
    }

    public void MarkAvailable()
    {
        Status = DriverStatus.Available;
    }

    public void GoOffline()
    {
        Status = DriverStatus.Offline;
    }

    public void UpdateLocation(Location location)
    {
        CurrentLocation = location
            ?? throw new InvalidMatchingException(
                "Driver location is required.");
    }
}