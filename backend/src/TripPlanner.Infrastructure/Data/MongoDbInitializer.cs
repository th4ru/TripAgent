using MongoDB.Driver;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Infrastructure.Data;

/// <summary>
/// Creates the indexes the application relies on and seeds the built-in roles.
/// Safe to run on every startup (idempotent).
/// </summary>
public class MongoDbInitializer
{
    private static readonly (Guid Id, string Name)[] SeedRoles =
    {
        (new Guid("11111111-1111-1111-1111-111111111111"), "User"),
        (new Guid("22222222-2222-2222-2222-222222222222"), "Admin")
    };

    private readonly MongoDbContext _context;

    public MongoDbInitializer(MongoDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _context.Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.UserName),
                new CreateIndexOptions { Unique = true, Name = "ux_users_user_name" }),
            cancellationToken: cancellationToken);

        await _context.Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_users_email" }),
            cancellationToken: cancellationToken);

        await _context.Roles.Indexes.CreateOneAsync(
            new CreateIndexModel<Role>(
                Builders<Role>.IndexKeys.Ascending(r => r.Name),
                new CreateIndexOptions { Unique = true, Name = "ux_roles_name" }),
            cancellationToken: cancellationToken);

        await _context.UserRoles.Indexes.CreateOneAsync(
            new CreateIndexModel<UserRole>(
                Builders<UserRole>.IndexKeys.Ascending(ur => ur.UserId).Ascending(ur => ur.RoleId),
                new CreateIndexOptions { Unique = true, Name = "ux_user_roles_user_id_role_id" }),
            cancellationToken: cancellationToken);

        await _context.Trips.Indexes.CreateOneAsync(
            new CreateIndexModel<Trip>(
                Builders<Trip>.IndexKeys.Ascending(t => t.UserId),
                new CreateIndexOptions { Name = "ix_trips_user_id" }),
            cancellationToken: cancellationToken);

        await _context.Trips.Indexes.CreateOneAsync(
            new CreateIndexModel<Trip>(
                Builders<Trip>.IndexKeys.Ascending(t => t.Status),
                new CreateIndexOptions { Name = "ix_trips_status" }),
            cancellationToken: cancellationToken);

        await _context.Trips.Indexes.CreateOneAsync(
            new CreateIndexModel<Trip>(
                Builders<Trip>.IndexKeys.Ascending(t => t.StartDate),
                new CreateIndexOptions { Name = "ix_trips_start_date" }),
            cancellationToken: cancellationToken);

        foreach ((Guid id, string name) in SeedRoles)
        {
            await _context.Roles.ReplaceOneAsync(
                r => r.Name == name,
                new Role { Id = id, Name = name },
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
    }
}
