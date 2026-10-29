using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TripPlanner.Domain.Enums;

namespace TripPlanner.Domain.Entities;

public class Trip
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int NumberOfTravelers { get; set; }
    public decimal Budget { get; set; }
    public string Currency { get; set; } = "LKR";
    public string TravelPreferences { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public TripStatus Status { get; set; } = TripStatus.Planning;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
