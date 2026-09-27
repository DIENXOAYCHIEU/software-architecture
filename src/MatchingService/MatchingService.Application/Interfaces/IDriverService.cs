using MatchingService.Application.DTOs;

namespace MatchingService.Application.Interfaces;

public interface IDriverService
{
    Task<DriverResponse> CreateAsync(
        CreateDriverRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverResponse?> GetByIdAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);
}