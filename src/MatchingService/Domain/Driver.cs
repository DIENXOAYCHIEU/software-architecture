using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace MatchingService.Domain;

public sealed class Driver
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = default!;

    [BsonElement("name")]
    public string Name { get; set; } = default!;

    [BsonElement("status")]
    public string Status { get; set; } = "Available";

    [BsonElement("location")]
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; } = default!;
}