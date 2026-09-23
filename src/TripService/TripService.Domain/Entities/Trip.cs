using System.Diagnostics.CodeAnalysis;
using TripService.Domain.ValueObjects;
using TripService.Domain.Enums;
using TripService.Domain.Exceptions;

namespace TripService.Domain.Entities;

public class Trip{
	public Guid Id {get; set;}
	public Guid RiderId {get; set;}
	public Guid? DriverId {get; set;}
	public required Location StartLocation {get; set;}
	public required Location EndLocation {get; set;}
	public decimal? AmountToPay {get; set;}
	public DateTime Created {get; set;}
	public DateTime Updated {get; set;}
	public TripStatus Status {get; set;}

	private Trip(){}

	[SetsRequiredMembers]
	public Trip(Guid riderId, 
		Location startLocation, 
		Location endLocation, 
		DateTime created
		){
		Id = Guid.NewGuid();
		RiderId = riderId;
		StartLocation = startLocation;
		EndLocation = endLocation;
		Created = created;
		Updated = created;
		Status = TripStatus.REQUESTED;
	}

	public void AssignDriver(Guid driverId){
		if(Status!=TripStatus.REQUESTED){
			throw new InvalidTripException("Chuyến đi này đã có tài xế khác");
		}
		DriverId = driverId;
		Status = TripStatus.ACCEPTED;
		Updated = DateTime.UtcNow;
	}

	public void PickRiderUp(){
		if(Status!=TripStatus.ACCEPTED){
			throw new InvalidTripException("Chuyến đi phải được chấp nhận trước");
		}
		Status = TripStatus.PICKED;
		Updated = DateTime.UtcNow;
	}

	public void DropRiderOff(){
		if(Status!=TripStatus.PICKED){
			throw new InvalidTripException("Chuyến đi phải có khách trước");
		}
		Status = TripStatus.DROPPED;
		Updated = DateTime.UtcNow;
	}
}