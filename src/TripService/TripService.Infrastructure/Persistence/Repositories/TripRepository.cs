using Microsoft.EntityFrameworkCore;
using TripService.Application.Interfaces;
using TripService.Domain.Entities;

namespace TripService.Infrastructure.Persistence.Repositories;

public class TripRepository : ITripRepository{

	private readonly TripDbContext _context;

	public TripRepository(TripDbContext context){
		_context = context;
	}

	public async Task AddAsync(Trip trip){
		await _context.Trips.AddAsync(trip);
	}

	public async Task<Trip?> GetByIdAsync(Guid id){
		return await _context.Trips.FirstOrDefaultAsync(trip =>trip.Id == id);
	}
}