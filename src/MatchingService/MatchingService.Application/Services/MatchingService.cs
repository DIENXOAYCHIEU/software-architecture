using MatchingService.Application.DTOs;
using MatchingService.Application.Interfaces;
using MatchingService.Domain.Entities;
using MatchingService.Domain.Exceptions;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Application.Services;

public class MatchingService : IMatchingService
{
    private readonly IMatchingRepository _matchingRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MatchingService(
        IMatchingRepository matchingRepository,
        IDriverRepository driverRepository,
        IUnitOfWork unitOfWork)
    {
        _matchingRepository = matchingRepository;
        _driverRepository = driverRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreatedMatchingResponse> CreateAsync(
        CreateMatchingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TripId == Guid.Empty)
        {
            throw new InvalidMatchingException(
                "TripId is required.");
        }

        var existingRequest =
            await _matchingRepository.GetByTripIdAsync(
                request.TripId,
                cancellationToken);

        if (existingRequest != null)
        {
            throw new InvalidOperationException(
                "A matching request already exists for this trip.");
        }

        var location = new Location(
            request.PickupLatitude,
            request.PickupLongitude);

        var entity = new MatchingRequest(
            request.TripId,
            location);

        await _matchingRepository.AddAsync(
            entity,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatedMatchingResponse
        {
            MatchingId = entity.Id,
            TripId = entity.TripId,
            Status = entity.Status.ToString()
        };
    }

    public async Task<MatchingResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity =
            await _matchingRepository.GetByIdAsync(
                id,
                cancellationToken);

        return entity == null
            ? null
            : MapToResponse(entity);
    }

    public async Task<MatchingResponse?> GetByTripIdAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        var entity =
            await _matchingRepository.GetByTripIdAsync(
                tripId,
                cancellationToken);

        return entity == null
            ? null
            : MapToResponse(entity);
    }

    public async Task<MatchingResponse> StartSearchingAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var matching =
            await GetEntityOrThrow(
                id,
                cancellationToken);

        matching.StartSearching();

        await _matchingRepository.UpdateAsync(
            matching,
            cancellationToken);

        var drivers =
            await _driverRepository.GetAvailableAsync(
                cancellationToken);

        if (drivers.Count == 0)
        {
            matching.MarkFailed();

            await _matchingRepository.UpdateAsync(
                matching,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToResponse(matching);
        }

        var nearestDriver =
            FindNearestDriver(
                matching.PickupLocation,
                drivers);

        matching.AssignDriver(
            nearestDriver.Id);

        nearestDriver.MarkBusy();

        await _driverRepository.UpdateAsync(
            nearestDriver,
            cancellationToken);

        await _matchingRepository.UpdateAsync(
            matching,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(matching);
    }

    public async Task<MatchingResponse> AssignDriverAsync(
        Guid id,
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var matching =
            await GetEntityOrThrow(
                id,
                cancellationToken);

        if (matching.Status == Domain.Enums.MatchingStatus.Pending)
        {
            matching.StartSearching();
        }

        var driver =
            await _driverRepository.GetByIdAsync(
                driverId,
                cancellationToken);

        if (driver == null)
        {
            throw new KeyNotFoundException(
                "Driver not found.");
        }

        matching.AssignDriver(driverId);

        driver.MarkBusy();

        await _driverRepository.UpdateAsync(
            driver,
            cancellationToken);

        await _matchingRepository.UpdateAsync(
            matching,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(matching);
    }

    public async Task<MatchingResponse> MarkFailedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var matching =
            await GetEntityOrThrow(
                id,
                cancellationToken);

        matching.MarkFailed();

        await _matchingRepository.UpdateAsync(
            matching,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(matching);
    }

    public async Task<MatchingResponse> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var matching =
            await GetEntityOrThrow(
                id,
                cancellationToken);

        matching.Cancel();

        await _matchingRepository.UpdateAsync(
            matching,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(matching);
    }

    private async Task<MatchingRequest> GetEntityOrThrow(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity =
            await _matchingRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException(
                $"Matching request '{id}' was not found.");
        }

        return entity;
    }

    private static Driver FindNearestDriver(
        Location pickup,
        List<Driver> drivers)
    {
        Driver? nearestDriver = null;
        double shortestDistance = double.MaxValue;

        foreach (var driver in drivers)
        {
            var distance = CalculateDistanceKm(
                pickup,
                driver.CurrentLocation);

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestDriver = driver;
            }
        }

        return nearestDriver
            ?? throw new InvalidOperationException(
                "No available driver found.");
    }

    private static double CalculateDistanceKm(
        Location startLocation, Location endLocation)
    {
        const double earthRadiusKm = 6371.0;

        var dLatitude = DegreesToRadians(
            endLocation.Latitude - startLocation.Latitude);

        var dLongitude = DegreesToRadians(
            endLocation.Longitude - startLocation.Longitude );

        var lat1 = DegreesToRadians(startLocation.Latitude);
        var lat2 = DegreesToRadians(endLocation.Latitude);

        var a =
            Math.Sin(dLatitude / 2) *
            Math.Sin(dLatitude / 2)
            +
            Math.Cos(lat1) *
            Math.Cos(lat2) *
            Math.Sin(dLongitude / 2) *
            Math.Sin(dLongitude / 2);

        var c =
            2 * Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(
        double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static MatchingResponse MapToResponse(
        MatchingRequest entity)
    {
        return new MatchingResponse
        {
            MatchingId = entity.Id,
            TripId = entity.TripId,
            DriverId = entity.DriverId,
            PickupLatitude =
                entity.PickupLocation.Latitude,
            PickupLongitude =
                entity.PickupLocation.Longitude,
            Status = entity.Status.ToString(),
            CreatedAt = entity.CreatedAt,
            MatchedAt = entity.MatchedAt
        };
    }
}
