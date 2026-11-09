using MatchingService.Application.DTOs;
using MatchingService.Application.Interfaces;
using MatchingService.Domain.Entities;
using MatchingService.Domain.ValueObjects;
using MatchingService.Domain.Enums;

namespace MatchingService.Application.Services;

public class DriverService : IDriverService
{
    private readonly IDriverRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DriverService(
        IDriverRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

    public async Task UpdateLocationAsync(
        Guid driverId,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var driver = await _repository.GetByIdAsync(
            driverId,
            cancellationToken);

        if (driver == null)
        {
            throw new KeyNotFoundException(
                $"Driver '{driverId}' was not found.");
        }

        driver.UpdateLocation(
            new Location(latitude, longitude));

        await _repository.UpdateAsync(
            driver,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(
        Guid driverId,
        DriverStatus status,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "Driver status is invalid.");
        }

        var driver = await _repository.GetByIdAsync(
            driverId,
            cancellationToken);

        if (driver == null)
        {
            throw new KeyNotFoundException(
                $"Driver '{driverId}' was not found.");
        }

        driver.UpdateStatus(status);

        await _repository.UpdateAsync(
            driver,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
