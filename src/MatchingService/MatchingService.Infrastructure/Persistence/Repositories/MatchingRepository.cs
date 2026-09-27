using MatchingService.Application.Interfaces;
using MatchingService.Domain.Entities;
using MongoDB.Driver;

namespace MatchingService.Infrastructure.Persistence.Repositories;

public class MatchingRepository : IMatchingRepository
{
    private readonly IMongoCollection<MatchingRequest>
        _collection;

    public MatchingRepository(
        MongoDbContext context)
    {
        _collection = context.MatchingRequests;
    }

    public async Task AddAsync(
        MatchingRequest matchingRequest,
        CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(
            matchingRequest,
            cancellationToken: cancellationToken);
    }

    public async Task<MatchingRequest?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task<MatchingRequest?> GetByTripIdAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.TripId == tripId)
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task UpdateAsync(
        MatchingRequest matchingRequest,
        CancellationToken cancellationToken = default)
    {
        await _collection.ReplaceOneAsync(
            x => x.Id == matchingRequest.Id,
            matchingRequest,
            cancellationToken: cancellationToken);
    }
}