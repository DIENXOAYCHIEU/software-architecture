using MatchingService.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MatchingService.Infrastructure.Persistence;

public static class MongoMappings
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        BsonSerializer.RegisterSerializer(
            new GuidSerializer(GuidRepresentation.Standard));

        if (!BsonClassMap.IsClassMapRegistered(
                typeof(Driver)))
        {
            BsonClassMap.RegisterClassMap<Driver>(
                map =>
                {
                    map.AutoMap();

                    map.MapIdMember(
                        x => x.Id);
                });
        }

        if (!BsonClassMap.IsClassMapRegistered(
                typeof(MatchingRequest)))
        {
            BsonClassMap.RegisterClassMap<MatchingRequest>(
                map =>
                {
                    map.AutoMap();

                    map.MapIdMember(
                        x => x.Id);
                });
        }

        _registered = true;
    }
}
