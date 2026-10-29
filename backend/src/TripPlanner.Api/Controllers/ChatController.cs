using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TripPlanner.Application.Chat;
using TripPlanner.Infrastructure.N8n;

namespace TripPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatAgent _chatAgent;
    private readonly N8nOptions _n8nOptions;

    public ChatController(IChatAgent chatAgent, IOptions<N8nOptions> n8nOptions)
    {
        _chatAgent = chatAgent;
        _n8nOptions = n8nOptions.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "A non-empty 'message' is required." });
        }

        if (string.IsNullOrWhiteSpace(_n8nOptions.WebhookUrl))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "The AI assistant is not configured yet. Set N8n:WebhookUrl in configuration." });
        }

        try
        {
            ChatReply reply = await _chatAgent.SendAsync(request, cancellationToken);
            return Ok(new { reply = reply.Reply, sessionId = reply.SessionId });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { error = "The AI assistant is currently unavailable.", detail = ex.Message });
        }
    }
}
