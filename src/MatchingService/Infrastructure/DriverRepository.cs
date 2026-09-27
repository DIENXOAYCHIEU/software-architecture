using MatchingService.Application;
using MatchingService.Domain;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace MatchingService.Infrastructure;

public sealed class DriverRepository
{
    private readonly IMongoCollection<Driver> _drivers;

    public DriverRepository(IMongoDatabase database)
    {
        _drivers = database.GetCollection<Driver>("drivers");
    }

    public async Task EnsureIndexesAsync()
    {
        var index = new CreateIndexModel<Driver>(
            Builders<Driver>.IndexKeys.Geo2DSphere(x => x.Location)
        );

        await _drivers.Indexes.CreateOneAsync(index);
    }

    public async Task<MatchResult?> FindNearestAvailableDriverAsync(
        MatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var geoNear = new BsonDocument("$geoNear",
            new BsonDocument
            {
                {
                    "near",
                    new BsonDocument
                    {
                        { "type", "Point" },
                        {
                            "coordinates",
                            new BsonArray
                            {
                                request.PickupLongitude,
                                request.PickupLatitude
                            }
                        }
                    }
                },
                { "key", "location" },
                { "distanceField", "distanceMeters" },
                { "spherical", true },
                {
                    "query",
                    new BsonDocument("status", "Available")
                },
                { "maxDistance", request.MaxDistanceMeters }
            });

        var limit = new BsonDocument("$limit", 1);

        var pipeline = new[]
        {
            geoNear,
            limit
        };

        var documents = await _drivers
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync(cancellationToken);

        if (documents.Count == 0)
        {
            return null;
        }

        var document = documents[0];

        var coordinates =
            document["location"]["coordinates"].AsBsonArray;

        double longitude = coordinates[0].ToDouble();
        double latitude = coordinates[1].ToDouble();

        return new MatchResult(
            Matched: true,
            DriverId: document["_id"].AsString,
            DriverName: document["name"].AsString,
            DriverLatitude: latitude,
            DriverLongitude: longitude,
            DistanceMeters: document["distanceMeters"].ToDouble()
        );
    }
}