using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripPlanner.Application.Auth;

namespace TripPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "A request body is required." });
        }

        try
        {
            AuthResult result = await _authService.RegisterAsync(request, cancellationToken);
            return Ok(ToResponse(result));
        }
        catch (AuthValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "A request body is required." });
        }

        try
        {
            AuthResult result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(ToResponse(result));
        }
        catch (AuthValidationException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        string? subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out Guid userId))
        {
            return Unauthorized(new { error = "Invalid token." });
        }

        AuthUser? user = await _authService.GetUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { error = "User not found." });
        }

        return Ok(new
        {
            id = user.Id,
            userName = user.UserName,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            roles = user.Roles
        });
    }

    private static object ToResponse(AuthResult result) => new
    {
        token = result.Token,
        expiresAtUtc = result.ExpiresAtUtc,
        user = new
        {
            id = result.User.Id,
            userName = result.User.UserName,
            email = result.User.Email,
            firstName = result.User.FirstName,
            lastName = result.User.LastName,
            roles = result.User.Roles
        }
    };
}
