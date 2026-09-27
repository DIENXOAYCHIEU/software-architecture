using MatchingService.Infrastructure;

namespace MatchingService.Application;

public sealed class MatchingService : IMatchingService
{
    private readonly DriverRepository _driverRepository;

    public MatchingService(DriverRepository driverRepository)
    {
        _driverRepository = driverRepository;
    }

    public async Task<MatchResult?> MatchAsync(
        MatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PickupLatitude is < -90 or > 90)
        {
            throw new ArgumentException("Invalid pickup latitude.");
        }

        if (request.PickupLongitude is < -180 or > 180)
        {
            throw new ArgumentException("Invalid pickup longitude.");
        }

        if (request.MaxDistanceMeters <= 0)
        {
            throw new ArgumentException(
                "MaxDistanceMeters must be greater than 0.");
        }

        return await _driverRepository.FindNearestAvailableDriverAsync(
            request,
            cancellationToken);
    }
}