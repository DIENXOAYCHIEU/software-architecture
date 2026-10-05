using TripService.Application.DTOs;
using TripService.Domain.Entities;

namespace TripService.Application.Interfaces;

public interface ITripService{
    Task<CreatedTripResponse> CreateTripAsync(CreatedTripRequest request);
    Task<Trip> GetByIdAsync(Guid tripId);
    Task<AssignedTripResponse> AssignDriverAsync(Guid tripId, Guid driverId);
    Task<AssignedTripResponse> UpdateStatusToDropped(Guid tripId);
    Task<AssignedTripResponse> UpdateStatusToPicked(Guid tripId);
}
