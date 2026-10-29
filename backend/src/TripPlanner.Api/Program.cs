using TripPlanner.Infrastructure;
using TripPlanner.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

const string corsPolicy = "AllowFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<MongoDbInitializer>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("MongoDb");

    try
    {
        await initializer.InitializeAsync();
        logger.LogInformation("MongoDB indexes and seed data are ready.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex,
            "MongoDB initialization failed. The API will still start; /api/health reports the connection status.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(corsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
