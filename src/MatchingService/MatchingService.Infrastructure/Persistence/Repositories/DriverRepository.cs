using MatchingService.Application.Interfaces;
using MatchingService.Domain.Entities;
using MatchingService.Domain.Enums;
using MongoDB.Driver;

namespace MatchingService.Infrastructure.Persistence.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly IMongoCollection<Driver> _collection;

    public DriverRepository(
        MongoDbContext context)
    {
        _collection = context.Drivers;
    }

    public async Task AddAsync(
        Driver driver,
        CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(
            driver,
            cancellationToken: cancellationToken);
    }

    public async Task<Driver?> GetByIdAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.Id == driverId)
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task<List<Driver>> GetAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.Status == DriverStatus.Available)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Driver driver,
        CancellationToken cancellationToken = default)
    {
        await _collection.ReplaceOneAsync(
            x => x.Id == driver.Id,
            driver,
            cancellationToken: cancellationToken);
    }
}