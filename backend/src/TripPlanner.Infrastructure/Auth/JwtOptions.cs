namespace TripPlanner.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TripPlanner.Api";

    public string Audience { get; set; } = "TripPlanner.Client";

    /// <summary>HMAC signing key. Must be at least 32 characters.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 1440;
}
