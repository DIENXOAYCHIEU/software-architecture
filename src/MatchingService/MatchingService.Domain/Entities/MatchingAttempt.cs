using MatchingService.Domain.Enums;

namespace MatchingService.Domain.Entities;

public class MatchingAttempt
{
    public Guid Id { get; private set; }

    public Guid MatchingRequestId { get; private set; }

    public Guid DriverId { get; private set; }

    public int AttemptNumber { get; private set; }

    public MatchingAttemptStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? RespondedAt { get; private set; }

    public MatchingRequest MatchingRequest { get; private set; } = null!;

    public Driver Driver { get; private set; } = null!;

    private MatchingAttempt()
    {
    }

    public MatchingAttempt(
        Guid matchingRequestId,
        Guid driverId,
        int attemptNumber)
    {
        Id = Guid.NewGuid();
        MatchingRequestId = matchingRequestId;
        DriverId = driverId;
        AttemptNumber = attemptNumber;
        Status = MatchingAttemptStatus.Offered;
        CreatedAt = DateTime.UtcNow;
    }

    public void Accept()
    {
        if (Status != MatchingAttemptStatus.Offered)
        {
            throw new InvalidOperationException(
                "This matching attempt is no longer active.");
        }

        Status = MatchingAttemptStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != MatchingAttemptStatus.Offered)
        {
            throw new InvalidOperationException(
                "This matching attempt is no longer active.");
        }

        Status = MatchingAttemptStatus.Rejected;
        RespondedAt = DateTime.UtcNow;
    }
}