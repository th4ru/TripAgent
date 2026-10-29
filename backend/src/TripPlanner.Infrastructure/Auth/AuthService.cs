using System.Net.Mail;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using TripPlanner.Application.Auth;
using TripPlanner.Domain.Entities;
using TripPlanner.Infrastructure.Data;

namespace TripPlanner.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private static readonly Guid DefaultUserRoleId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Regex UserNamePattern = new("^[a-zA-Z0-9._-]{3,40}$", RegexOptions.Compiled);

    private readonly MongoDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        MongoDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        string userName = (request.UserName ?? string.Empty).Trim();
        string email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        string password = request.Password ?? string.Empty;

        if (!UserNamePattern.IsMatch(userName))
        {
            throw new AuthValidationException(
                "Username must be 3-40 characters and may only contain letters, numbers, dots, dashes and underscores.");
        }

        if (!IsValidEmail(email))
        {
            throw new AuthValidationException("Enter a valid email address.");
        }

        if (password.Length < 8)
        {
            throw new AuthValidationException("Password must be at least 8 characters long.");
        }

        if (password.Length > 128)
        {
            throw new AuthValidationException("Password must be 128 characters or fewer.");
        }

        bool duplicate = await ExistsAsync(userName, email, cancellationToken);
        if (duplicate)
        {
            throw new AuthValidationException("An account with that username or email already exists.");
        }

        DateTime now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            FirstName = (request.FirstName ?? string.Empty).Trim(),
            LastName = (request.LastName ?? string.Empty).Trim(),
            PasswordHash = _passwordHasher.Hash(password),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            await _context.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new AuthValidationException("An account with that username or email already exists.");
        }

        Role role = await GetOrCreateDefaultRoleAsync(cancellationToken);
        await _context.UserRoles.InsertOneAsync(
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id
            },
            cancellationToken: cancellationToken);

        return BuildResult(user, new[] { role.Name });
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        string login = (request.EmailOrUserName ?? string.Empty).Trim();
        string password = request.Password ?? string.Empty;

        if (login.Length == 0 || password.Length == 0)
        {
            throw new AuthValidationException("Enter your email/username and password.");
        }

        string pattern = "^" + Regex.Escape(login) + "$";
        FilterDefinition<User> filter = Builders<User>.Filter.Or(
            Builders<User>.Filter.Regex(u => u.Email, new BsonRegularExpression(pattern, "i")),
            Builders<User>.Filter.Regex(u => u.UserName, new BsonRegularExpression(pattern, "i")));

        User? user = await _context.Users.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            throw new AuthValidationException("Incorrect email/username or password.");
        }

        if (!user.IsActive)
        {
            throw new AuthValidationException("This account has been disabled. Contact the administrator.");
        }

        IReadOnlyList<string> roles = await LoadRolesAsync(user.Id, cancellationToken);
        return BuildResult(user, roles);
    }

    public async Task<AuthUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        User? user = await _context.Users
            .Find(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        IReadOnlyList<string> roles = await LoadRolesAsync(user.Id, cancellationToken);
        return ToAuthUser(user, roles);
    }

    private async Task<bool> ExistsAsync(string userName, string email, CancellationToken cancellationToken)
    {
        FilterDefinition<User> filter = Builders<User>.Filter.Or(
            Builders<User>.Filter.Regex(u => u.Email, new BsonRegularExpression("^" + Regex.Escape(email) + "$", "i")),
            Builders<User>.Filter.Regex(u => u.UserName, new BsonRegularExpression("^" + Regex.Escape(userName) + "$", "i")));

        return await _context.Users.Find(filter).AnyAsync(cancellationToken);
    }

    private async Task<Role> GetOrCreateDefaultRoleAsync(CancellationToken cancellationToken)
    {
        Role? role = await _context.Roles
            .Find(r => r.Name == "User")
            .FirstOrDefaultAsync(cancellationToken);

        if (role is not null)
        {
            return role;
        }

        role = new Role { Id = DefaultUserRoleId, Name = "User" };

        try
        {
            await _context.Roles.ReplaceOneAsync(
                r => r.Name == "User",
                role,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
        catch (MongoWriteException)
        {
            // Race with the startup seed: re-read the role.
            role = await _context.Roles.Find(r => r.Name == "User").FirstOrDefaultAsync(cancellationToken)
                ?? role;
        }

        return role;
    }

    private async Task<IReadOnlyList<string>> LoadRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        List<UserRole> links = await _context.UserRoles
            .Find(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken);

        if (links.Count == 0)
        {
            return Array.Empty<string>();
        }

        List<Guid> roleIds = links.Select(link => link.RoleId).Distinct().ToList();

        List<Role> roles = await _context.Roles
            .Find(Builders<Role>.Filter.In(r => r.Id, roleIds))
            .ToListAsync(cancellationToken);

        return roles
            .Select(r => r.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();
    }

    private AuthResult BuildResult(User user, IReadOnlyList<string> roles)
    {
        AuthUser authUser = ToAuthUser(user, roles);
        (string token, DateTime expiresAtUtc) = _tokenService.CreateToken(authUser);
        return new AuthResult(token, expiresAtUtc, authUser);
    }

    private static AuthUser ToAuthUser(User user, IReadOnlyList<string> roles) => new(
        user.Id,
        user.UserName,
        user.Email,
        user.FirstName,
        user.LastName,
        roles);

    private static bool IsValidEmail(string email)
    {
        if (email.Length is 0 or > 256)
        {
            return false;
        }

        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
