namespace CodeCafe.Application.AI;

public sealed record AgentRunResult(
    string ProfileId,
    string Text,
    AgentSessionSnapshot Session);
