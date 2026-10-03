using MatchingService.Application.Interfaces;
using MatchingService.Domain.Entities;
using MatchingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
        await _context.SaveChangesAsync(cancellationToken); // Bắt buộc phải có để lưu vào MySQL
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
        await _context.SaveChangesAsync(cancellationToken); // Bắt buộc phải có để cập nhật vào MySQL
    }
}