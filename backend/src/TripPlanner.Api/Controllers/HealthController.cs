using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using TripPlanner.Infrastructure.Data;

namespace TripPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly MongoDbContext _db;

    public HealthController(MongoDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            await _db.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);

            return Ok(new { status = "healthy", database = "connected", provider = "mongodb" });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "unhealthy", database = "unreachable", error = ex.Message });
        }
    }
}
