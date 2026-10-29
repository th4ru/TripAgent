using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TripPlanner.Application.Auth;
using TripPlanner.Application.Chat;
using TripPlanner.Infrastructure.Auth;
using TripPlanner.Infrastructure.Data;
using TripPlanner.Infrastructure.N8n;

namespace TripPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddMongoDb(services, configuration);
        AddAuth(services, configuration);
        AddN8n(services, configuration);

        return services;
    }

    private static void AddMongoDb(IServiceCollection services, IConfiguration configuration)
    {
        MongoDbSettings settings = configuration
            .GetSection(MongoDbSettings.SectionName)
            .Get<MongoDbSettings>() ?? new MongoDbSettings();

        if (string.IsNullOrWhiteSpace(settings.ConnectionString) ||
            settings.ConnectionString.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "MongoDb:ConnectionString is not configured. Set it in appsettings.Development.json " +
                "or through the MongoDb__ConnectionString environment variable.");
        }

        if (string.IsNullOrWhiteSpace(settings.DatabaseName))
        {
            settings.DatabaseName = "ai_trip_planner";
        }

        services.AddSingleton(settings);
        services.AddSingleton<IMongoClient>(_ =>
        {
            var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            return new MongoClient(clientSettings);
        });
        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<MongoDbInitializer>();
    }

    private static void AddAuth(IServiceCollection services, IConfiguration configuration)
    {
        JwtOptions jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.Key) ||
            jwt.Key.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Jwt:Key is not configured. Set it in appsettings.Development.json or through the Jwt__Key " +
                "environment variable. Use a random string of at least 32 characters.");
        }

        if (jwt.Key.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");
        }

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
    }

    private static void AddN8n(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<N8nOptions>(configuration.GetSection(N8nOptions.SectionName));

        services.AddHttpClient<IChatAgent, N8nChatAgent>((sp, client) =>
        {
            N8nOptions options = sp.GetRequiredService<IOptions<N8nOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 60);
        });
    }
}
