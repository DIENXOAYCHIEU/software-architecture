using TripService.Domain.ValueObjects;
using TripService.Domain.Enums;

namespace TripService.Application.DTOs;

public class CreatedTripResponse{
	public Guid Id {get; set;}
	public Guid RiderId {get; set;}
	public TripStatus Status {get; set;}

	public CreatedTripResponse(){}

	public CreatedTripResponse(Guid id, Guid riderId, TripStatus status){
		Id=id;
		RiderId=riderId;
		Status=status;
	}
}