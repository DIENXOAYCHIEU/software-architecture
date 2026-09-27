namespace MatchingService.Application;

public interface IMatchingService
{
    Task<MatchResult?> MatchAsync(
        MatchRequest request,
        CancellationToken cancellationToken = default);
}