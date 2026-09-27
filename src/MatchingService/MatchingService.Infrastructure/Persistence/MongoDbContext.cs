using MatchingService.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MatchingService.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    private readonly MongoDbSettings _settings;

    public MongoDbContext(
        IOptions<MongoDbSettings> settings)
    {
        _settings = settings.Value;

        MongoMappings.Register();

        var client = new MongoClient(
            _settings.ConnectionString);

        _database = client.GetDatabase(
            _settings.DatabaseName);
    }

    public IMongoCollection<MatchingRequest> MatchingRequests
        => _database.GetCollection<MatchingRequest>(
            _settings.MatchingCollectionName);

    public IMongoCollection<Driver> Drivers
        => _database.GetCollection<Driver>(
            _settings.DriverCollectionName);
}