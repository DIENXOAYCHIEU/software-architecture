using TripService.Domain.Entities;
using TripService.Domain.ValueObjects;

namespace TripService.Application.Interfaces;

public interface ITripRepository{
	Task AddAsync(Trip trip);
	Task<Trip?> GetByIdAsync(Guid id);
}
