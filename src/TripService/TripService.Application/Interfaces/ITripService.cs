using TripService.Application.DTOs;

namespace TripService.Application.Interfaces;

public interface ITripService{
    Task<CreatedTripResponse> CreateTripAsync(CreatedTripRequest request);
}
