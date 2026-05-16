namespace CodeCafe.Application.AI;

public sealed record AgentRunUpdate(
    AgentRunUpdateKind Kind,
    string? Text = null,
    AgentSessionSnapshot? Session = null);
