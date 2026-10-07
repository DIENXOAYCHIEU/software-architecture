using MatchingService.Application.DTOs;

namespace MatchingService.Application.Interfaces;

public interface IMatchingService
{
    Task<CreatedMatchingResponse> CreateAsync(
        CreateMatchingRequest request,
        CancellationToken cancellationToken = default);

    Task<MatchingResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MatchingResponse?> GetByTripIdAsync(
        Guid tripId,
        CancellationToken cancellationToken = default);

    Task<MatchingResponse> StartMatchingAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MatchingResponse> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MatchingResponse> AcceptDriverAsync(
        Guid matchingId,
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<MatchingResponse> RejectDriverAsync(
        Guid matchingId,
        Guid driverId,
        CancellationToken cancellationToken = default);
}