using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TripPlanner.Application.Chat;
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
        services.AddSingleton<IMongoClient>(_ => new MongoClient(settings.ConnectionString));
        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<MongoDbInitializer>();
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
