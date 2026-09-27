namespace MatchingService.Infrastructure.Persistence;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public string MatchingCollectionName { get; set; }
        = "matching_requests";

    public string DriverCollectionName { get; set; }
        = "drivers";
}