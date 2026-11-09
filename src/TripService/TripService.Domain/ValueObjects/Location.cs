namespace TripService.Domain.ValueObjects;

public class Location{
	public float Latitude {get; set;}
	public float Longitude {get; set;}
	public string Address {get; set;} = string.Empty;

	private Location(){}

	public Location(float latitude, float longitude, string address){
		Latitude=latitude;
		Longitude = longitude;
		Address = address;
	}
}