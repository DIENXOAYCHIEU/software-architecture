using MatchingService.Application.DTOs;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Application.Interfaces;

public interface IRoutingService
{
    Task<RouteResult> CalculateRouteAsync(
        Location origin,
        Location destination,
        CancellationToken cancellationToken = default);
}