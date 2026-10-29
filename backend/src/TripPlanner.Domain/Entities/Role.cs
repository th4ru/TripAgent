using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TripPlanner.Domain.Entities;

public class Role
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
