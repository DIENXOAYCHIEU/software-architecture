using TripService.Domain.ValueObjects;

namespace TripService.Application.DTOs;

public class CreatedTripRequest{

	public Guid RiderId {get; set;}
	public required Location StartLocation {get; set;}
	public required Location EndLocation {get; set;}
}
