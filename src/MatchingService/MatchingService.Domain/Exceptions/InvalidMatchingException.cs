namespace MatchingService.Domain.Exceptions;

public class InvalidMatchingException : Exception
{
    public InvalidMatchingException(string message)
        : base(message)
    {
    }
}