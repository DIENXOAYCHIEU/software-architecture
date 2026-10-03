using MatchingService.Application.DTOs;

namespace MatchingService.Application.Interfaces;

public interface IPricingService
{
    Task<PricingResponse> CalculateAsync(
        PricingRequest request,
        CancellationToken cancellationToken = default);
}