using CodeCafe.Application.AI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace CodeCafe.Infrastructure.AI.Maf;

internal sealed class MafAgentRuntime(IMafAgentFactory agentFactory) : IAgentRuntime
{
    public async Task<AgentRunResult> RunAsync(
        AgentRunRequest request,
        CancellationToken cancellationToken)
    {
        var agent = agentFactory.CreateAgent(request.ProfileId);
        var session = await CreateSessionAsync(agent, request.Session, cancellationToken);
        var response = await agent.RunAsync(
            ToMafMessage(request.Message),
            session,
            cancellationToken: cancellationToken);
        var snapshot = await CreateSnapshotAsync(agent, session, cancellationToken);

        return new AgentRunResult(request.ProfileId, response.Text, snapshot);
    }

    public async IAsyncEnumerable<AgentRunUpdate> RunStreamingAsync(
        AgentRunRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var agent = agentFactory.CreateAgent(request.ProfileId);
        var session = await CreateSessionAsync(agent, request.Session, cancellationToken);

        await foreach (var update in agent
            .RunStreamingAsync(ToMafMessage(request.Message), session, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return new AgentRunUpdate(AgentRunUpdateKind.MessageDelta, update.Text);
            }
        }

        var snapshot = await CreateSnapshotAsync(agent, session, cancellationToken);
        yield return new AgentRunUpdate(AgentRunUpdateKind.Completed, Session: snapshot);
    }

    private static ChatMessage ToMafMessage(AgentMessage message)
    {
        var role = message.Role switch
        {
            AgentMessageRole.Assistant => ChatRole.Assistant,
            AgentMessageRole.System => ChatRole.System,
            _ => ChatRole.User,
        };

        return new ChatMessage(role, message.Content);
    }

    private static async ValueTask<AgentSession> CreateSessionAsync(
        AIAgent agent,
        AgentSessionSnapshot? snapshot,
        CancellationToken cancellationToken)
    {
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.SerializedState))
        {
            return await agent.CreateSessionAsync(cancellationToken);
        }

        using var document = JsonDocument.Parse(snapshot.SerializedState);
        return await agent.DeserializeSessionAsync(document.RootElement, cancellationToken: cancellationToken);
    }

    private static async ValueTask<AgentSessionSnapshot> CreateSnapshotAsync(
        AIAgent agent,
        AgentSession session,
        CancellationToken cancellationToken)
    {
        var serializedSession = await agent.SerializeSessionAsync(session, cancellationToken: cancellationToken);

        return new AgentSessionSnapshot(serializedSession.GetRawText());
    }
}
