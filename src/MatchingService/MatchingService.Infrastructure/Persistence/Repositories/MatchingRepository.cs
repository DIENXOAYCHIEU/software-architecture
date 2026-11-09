using MatchingService.Application.Interfaces;
using MatchingService.Domain.Entities;
using MatchingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MatchingService.Domain.Enums;

namespace MatchingService.Infrastructure.Persistence.Repositories;

public class MatchingRepository : IMatchingRepository
{
    private readonly MatchingDbContext _context;

    public MatchingRepository(MatchingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        MatchingRequest matchingRequest,
        CancellationToken cancellationToken = default)
    {
        // Sử dụng trực tiếp qua _context.MatchingRequests
        await _context.MatchingRequests.AddAsync(matchingRequest, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MatchingRequest?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.MatchingRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<MatchingRequest?> GetByTripIdAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MatchingRequests
            .FirstOrDefaultAsync(x => x.TripId == tripId, cancellationToken);
    }

    public async Task UpdateAsync(
        MatchingRequest matchingRequest,
        CancellationToken cancellationToken = default)
    {
        _context.MatchingRequests.Update(matchingRequest);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAttemptAsync(
        MatchingAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        await _context.MatchingAttempts.AddAsync(
            attempt,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MatchingAttempt?> GetAttemptAsync(
        Guid matchingId,
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MatchingAttempts
            .FirstOrDefaultAsync(
                x =>
                    x.MatchingRequestId == matchingId &&
                    x.DriverId == driverId &&
                    x.Status == MatchingAttemptStatus.Offered,
                cancellationToken);
    }

    public async Task<List<MatchingAttempt>>
    GetAttemptsByMatchingIdAsync(
        Guid matchingId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MatchingAttempts
            .Where(x => x.MatchingRequestId == matchingId)
            .OrderBy(x => x.AttemptNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAttemptAsync(
        MatchingAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        _context.MatchingAttempts.Update(attempt);
        await _context.SaveChangesAsync(cancellationToken);
    }
}