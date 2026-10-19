namespace TripService.Application.Interfaces;

public interface ITripEventPublisher
{
    Task PublishAsync(string eventName, object payload, CancellationToken ct = default);
}
