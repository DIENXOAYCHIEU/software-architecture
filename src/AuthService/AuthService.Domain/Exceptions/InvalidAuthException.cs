namespace AuthService.Domain.Exceptions;

public class InvalidAuthException : Exception{
	public InvalidAuthException(String message) : base(message){}
}