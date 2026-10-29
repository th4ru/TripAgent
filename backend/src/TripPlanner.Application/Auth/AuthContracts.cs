namespace TripPlanner.Application.Auth;

public sealed record RegisterRequest(
    string UserName,
    string Email,
    string Password,
    string? FirstName,
    string? LastName);

public sealed record LoginRequest(string EmailOrUserName, string Password);

public sealed record AuthUser(
    Guid Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles);

public sealed record AuthResult(string Token, DateTime ExpiresAtUtc, AuthUser User);

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(AuthUser user);
}

/// <summary>Thrown when user input is invalid. Controllers translate this into HTTP 400.</summary>
public class AuthValidationException : Exception
{
    public AuthValidationException(string message) : base(message)
    {
    }
}
