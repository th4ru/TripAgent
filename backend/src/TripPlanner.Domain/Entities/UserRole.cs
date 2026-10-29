using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TripPlanner.Domain.Entities;

public class UserRole
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid RoleId { get; set; }
}
