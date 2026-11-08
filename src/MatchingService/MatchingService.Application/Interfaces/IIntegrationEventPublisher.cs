namespace MatchingService.Application.Interfaces;

public interface IIntegrationEventPublisher
{
    Task PublishDriverAssignedAsync(
        Guid tripId,
        Guid driverId,
        CancellationToken cancellationToken = default);
}