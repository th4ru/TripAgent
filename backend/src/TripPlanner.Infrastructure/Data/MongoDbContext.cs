using MongoDB.Driver;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, MongoDbSettings settings)
    {
        _database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Role> Roles => _database.GetCollection<Role>("roles");
    public IMongoCollection<UserRole> UserRoles => _database.GetCollection<UserRole>("user_roles");
    public IMongoCollection<Trip> Trips => _database.GetCollection<Trip>("trips");
}
