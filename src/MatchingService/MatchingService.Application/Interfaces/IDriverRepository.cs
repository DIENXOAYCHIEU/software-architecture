using MatchingService.Domain.Entities;

namespace MatchingService.Application.Interfaces;

public interface IDriverRepository
{
    Task AddAsync(
        Driver driver,
        CancellationToken cancellationToken = default);

    Task<Driver?> GetByIdAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<List<Driver>> GetAvailableAsync(
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Driver driver,
        CancellationToken cancellationToken = default);
}