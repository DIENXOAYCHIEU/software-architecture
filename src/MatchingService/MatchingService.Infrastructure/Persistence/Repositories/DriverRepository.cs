using MatchingService.Application.Interfaces;
using MatchingService.Domain.Entities;
using MatchingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchingService.Infrastructure.Persistence.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly MatchingDbContext _context;

    public DriverRepository(MatchingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Driver driver,
        CancellationToken cancellationToken = default)
    {
        await _context.Drivers.AddAsync(
            driver,
            cancellationToken);

    }

    public async Task<Driver?> GetByIdAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Drivers
            .FirstOrDefaultAsync(
                x => x.Id == driverId,
                cancellationToken);
    }

    public async Task<List<Driver>> GetAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Drivers
            .Where(x => x.Status == Domain.Enums.DriverStatus.Available)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Driver driver,
        CancellationToken cancellationToken = default)
    {
        _context.Drivers.Update(driver);

    }
}
