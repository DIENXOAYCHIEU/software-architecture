namespace TripService.Domain.Exceptions;

public class InvalidTripException : Exception{
	public InvalidTripException(String message) : base(message){}
}