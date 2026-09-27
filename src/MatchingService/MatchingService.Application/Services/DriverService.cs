using MatchingService.Application.DTOs;
using MatchingService.Application.Interfaces;
using MatchingService.Domain.Entities;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Application.Services;

public class DriverService : IDriverService
{
    private readonly IDriverRepository _repository;

    public DriverService(
        IDriverRepository repository)
    {
        _repository = repository;
    }

    public async Task<DriverResponse> CreateAsync(
        CreateDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = new Location(
            request.Latitude,
            request.Longitude);

        var driver = new Driver(
            request.Name,
            location);

        await _repository.AddAsync(
            driver,
            cancellationToken);

        return MapToResponse(driver);
    }

    public async Task<DriverResponse?> GetByIdAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var driver =
            await _repository.GetByIdAsync(
                driverId,
                cancellationToken);

        return driver == null
            ? null
            : MapToResponse(driver);
    }

    private static DriverResponse MapToResponse(
        Driver driver)
    {
        return new DriverResponse
        {
            DriverId = driver.Id,
            Name = driver.Name,
            Latitude = driver.CurrentLocation.Latitude,
            Longitude = driver.CurrentLocation.Longitude,
            Status = driver.Status.ToString()
        };
    }
}