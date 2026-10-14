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

    Task AddAttemptAsync(
        MatchingAttempt attempt,
        CancellationToken cancellationToken = default);

    Task<MatchingAttempt?> GetAttemptAsync(
        Guid matchingId,
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<List<MatchingAttempt>> GetAttemptsByMatchingIdAsync(
        Guid matchingId,
        CancellationToken cancellationToken = default);

    Task UpdateAttemptAsync(
        MatchingAttempt attempt,
        CancellationToken cancellationToken = default);
}