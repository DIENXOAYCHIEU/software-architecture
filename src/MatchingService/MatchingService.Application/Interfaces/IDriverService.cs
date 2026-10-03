using MatchingService.Application.DTOs;
using MatchingService.Domain.Enums;

namespace MatchingService.Application.Interfaces;

public interface IDriverService
{
    Task<DriverResponse> CreateAsync(
        CreateDriverRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverResponse?> GetByIdAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task UpdateLocationAsync(
        Guid driverId,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        Guid driverId,
        DriverStatus status,
        CancellationToken cancellationToken = default);
}