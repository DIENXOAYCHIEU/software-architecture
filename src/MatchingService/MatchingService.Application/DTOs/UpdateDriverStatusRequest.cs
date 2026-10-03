using MatchingService.Domain.Enums;

namespace MatchingService.Application.DTOs;

public class UpdateDriverStatusRequest
{
    public DriverStatus Status { get; set; }
}
