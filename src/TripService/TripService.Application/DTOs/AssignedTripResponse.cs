using TripService.Domain.Enums;

namespace TripService.Application.DTOs;

public class AssignedTripResponse{
	public Guid	Id {get; set;}
	public Guid? DriverId {get; set;}
	public TripStatus Status {get; set;}

	public AssignedTripResponse(){}
	public AssignedTripResponse(Guid id, Guid? driverId, TripStatus status){
		Id=id;
		DriverId = driverId;
		Status= status;
	}
}