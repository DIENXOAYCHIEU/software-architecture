using TripService.Application.Interfaces;
using TripService.Application.DTOs;
using TripService.Domain.Entities;
using TripService.Domain.Exceptions;

namespace TripService.Application.Services;

public class TripService : ITripService{
	private readonly ITripRepository _tripRepository;
	private readonly IUnitOfWork _unitOfWork;

	public TripService(ITripRepository tripRepository, IUnitOfWork unitOfWork){
		_tripRepository = tripRepository;
		_unitOfWork = unitOfWork;
	}	

	public async Task<Trip> GetByIdAsync(Guid tripId){
		var trip = await _tripRepository.GetByIdAsync(tripId);
		if (trip==null){
			throw new KeyNotFoundException($"Chuyến đi {tripId} không tồn tại.");
		}
		return trip;
	}

	public async Task<CreatedTripResponse> CreateTripAsync(CreatedTripRequest request){
		var trip = new Trip(
			request.RiderId,
			request.StartLocation,
			request.EndLocation,
			DateTime.UtcNow
			);

		await _tripRepository.AddAsync(trip);
		await _unitOfWork.SaveChangesAsync();

		return new CreatedTripResponse(
			trip.Id,
			trip.RiderId,
			trip.Status
			);
	}

	public async Task<AssignedTripResponse> AssignDriverAsync(Guid tripId, Guid driverId){
		var trip = await _tripRepository.GetByIdAsync(tripId);
		if (trip==null)
			throw new InvalidTripException("Chuyến đi không tồn tại");
		trip.AssignDriver(driverId);
		await _unitOfWork.SaveChangesAsync();

		return new AssignedTripResponse(
			trip.Id,
			trip.DriverId,
			trip.Status
			);
	}

	public async Task<AssignedTripResponse> UpdateStatusToPicked(Guid tripId){
		var trip = await _tripRepository.GetByIdAsync(tripId);
		if (trip==null)
			throw new InvalidTripException("Chuyến đi không tồn tại");

		trip.PickRiderUp();
		await _unitOfWork.SaveChangesAsync();

		return new AssignedTripResponse(
			trip.Id,
			trip.DriverId,
			trip.Status
			);

	}
	public async Task<AssignedTripResponse> UpdateStatusToDropped(Guid tripId){
		var trip = await _tripRepository.GetByIdAsync(tripId);
		if (trip==null)
			throw new InvalidTripException("Chuyến đi không tồn tại");

		trip.DropRiderOff();
		await _unitOfWork.SaveChangesAsync();

		return new AssignedTripResponse(
			trip.Id,
			trip.DriverId,
			trip.Status
			);

	}

}