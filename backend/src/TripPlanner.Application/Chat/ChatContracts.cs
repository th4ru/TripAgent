namespace TripPlanner.Application.Chat;

public sealed record ChatRequest(string Message, string? SessionId);

public sealed record ChatReply(string Reply, string? SessionId);

public interface IChatAgent
{
    Task<ChatReply> SendAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
