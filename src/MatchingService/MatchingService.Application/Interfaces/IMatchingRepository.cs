using MatchingService.Domain.Entities;

namespace MatchingService.Application.Interfaces;

public interface IMatchingRepository
{
    Task AddAsync(
        MatchingRequest matchingRequest,
        CancellationToken cancellationToken = default);

    Task<MatchingRequest?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MatchingRequest?> GetByTripIdAsync(
        Guid tripId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        MatchingRequest matchingRequest,
        CancellationToken cancellationToken = default);
}