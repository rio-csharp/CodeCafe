namespace CodeCafe.Application.AI;

public interface IAgentRuntime
{
    Task<AgentRunResult> RunAsync(
        AgentRunRequest request,
        CancellationToken cancellationToken);

    IAsyncEnumerable<AgentRunUpdate> RunStreamingAsync(
        AgentRunRequest request,
        CancellationToken cancellationToken);
}
