namespace TripPlanner.Infrastructure.Data;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "ai_trip_planner";
}
