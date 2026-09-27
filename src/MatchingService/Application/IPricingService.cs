namespace MatchingService.Application;

public interface IPricingService
{
    QuoteResult CalculateQuote(QuoteRequest request);
}