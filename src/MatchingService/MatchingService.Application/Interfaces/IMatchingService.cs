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

    Task<MatchingResponse> StartSearchingAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MatchingResponse> AssignDriverAsync(
        Guid id,
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<MatchingResponse> MarkFailedAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MatchingResponse> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}