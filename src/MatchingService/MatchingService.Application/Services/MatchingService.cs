using MatchingService.Application.DTOs;
using MatchingService.Application.Interfaces;
using MatchingService.Domain.Entities;
using MatchingService.Domain.Exceptions;
using MatchingService.Domain.ValueObjects;
using MatchingService.Domain.Enums;

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

    public async Task<MatchingResponse> StartMatchingAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var matching = await GetEntityOrThrow(
            id,
            cancellationToken);

        matching.StartSearching();

        return await FindAndOfferDriverAsync(
            matching,
            cancellationToken);
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

    private async Task<MatchingResponse> FindAndOfferDriverAsync(
        MatchingRequest matching,
        CancellationToken cancellationToken)
    {
        if (matching.AttemptCount >= matching.MaxAttempts)
        {
            matching.MarkFailed();

            await _matchingRepository.UpdateAsync(
                matching,
                cancellationToken);

            return MapToResponse(matching);
        }

        var attempts =
            await _matchingRepository.GetAttemptsByMatchingIdAsync(
                matching.Id,
                cancellationToken);

        var attemptedDriverIds =
            attempts
                .Select(x => x.DriverId)
                .ToHashSet();

        var availableDrivers =
            await _driverRepository.GetAvailableAsync(
                cancellationToken);

        var candidates = availableDrivers
            .Where(driver =>
                driver.CurrentLocation != null &&
                !attemptedDriverIds.Contains(driver.Id))
            .OrderBy(driver =>
                CalculateDistanceKm(
                    matching.PickupLocation,
                    driver.CurrentLocation!))
            .ToList();

        if (candidates.Count == 0)
        {
            matching.MarkFailed();

            await _matchingRepository.UpdateAsync(
                matching,
                cancellationToken);

            return MapToResponse(matching);
        }

        var nearestDriver = candidates[0];

        matching.IncrementAttempt();

        matching.OfferDriver(nearestDriver.Id);

        var attempt = new MatchingAttempt(
            matching.Id,
            nearestDriver.Id,
            matching.AttemptCount);

        await _matchingRepository.AddAttemptAsync(
            attempt,
            cancellationToken);

        await _matchingRepository.UpdateAsync(
            matching,
            cancellationToken);

        return MapToResponse(matching);
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

            AttemptCount = entity.AttemptCount,
            MaxAttempts = entity.MaxAttempts,

            CreatedAt = entity.CreatedAt,
            MatchedAt = entity.MatchedAt
        };
    }

    public async Task<MatchingResponse> AcceptDriverAsync(
        Guid matchingId,
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var matching =
            await GetEntityOrThrow(
                matchingId,
                cancellationToken);

        var attempt =
            await _matchingRepository.GetAttemptAsync(
                matchingId,
                driverId,
                cancellationToken);

        if (attempt == null)
        {
            throw new KeyNotFoundException(
                "Active matching attempt was not found.");
        }

        if (matching.DriverId != driverId)
        {
            throw new InvalidOperationException(
                "This driver is not the current offered driver.");
        }

        if (matching.Status == MatchingStatus.Matched)
        {
            throw new InvalidOperationException(
                "Matching has already been completed.");
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

        if (driver.Status != DriverStatus.Available)
        {
            throw new InvalidOperationException(
                "Driver is no longer available.");
        }

        attempt.Accept();

        matching.AssignDriver(driverId);

        driver.MarkBusy();

        await _matchingRepository.UpdateAttemptAsync(
            attempt,
            cancellationToken);

        await _driverRepository.UpdateAsync(
            driver,
            cancellationToken);

        await _matchingRepository.UpdateAsync(
            matching,
            cancellationToken);

        return MapToResponse(matching);
    }

    public async Task<MatchingResponse> RejectDriverAsync(
        Guid matchingId,
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var matching =
            await GetEntityOrThrow(
                matchingId,
                cancellationToken);

        var attempt =
            await _matchingRepository.GetAttemptAsync(
                matchingId,
                driverId,
                cancellationToken);

        if (attempt == null)
        {
            throw new KeyNotFoundException(
                "Active matching attempt was not found.");
        }

        if (matching.DriverId != driverId)
        {
            throw new InvalidOperationException(
                "This driver is not the current offered driver.");
        }

        attempt.Reject();

        matching.ClearDriver();

        await _matchingRepository.UpdateAttemptAsync(
            attempt,
            cancellationToken);

        await _matchingRepository.UpdateAsync(
            matching,
            cancellationToken);

        if (matching.AttemptCount >= matching.MaxAttempts)
        {
            matching.MarkFailed();

            await _matchingRepository.UpdateAsync(
                matching,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return MapToResponse(matching);
        }

        return await FindAndOfferDriverAsync(
            matching,
            cancellationToken);
    }
}
