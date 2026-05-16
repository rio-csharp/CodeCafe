namespace CodeCafe.Application.AI;

public sealed record AgentRunRequest(
    string ProfileId,
    AgentMessage Message,
    AgentSessionSnapshot? Session = null);
