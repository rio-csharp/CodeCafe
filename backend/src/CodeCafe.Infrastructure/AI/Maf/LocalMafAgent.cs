using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace CodeCafe.Infrastructure.AI.Maf;

internal sealed class LocalMafAgent(string profileId) : AIAgent
{
    public override string? Name => profileId;

    public override string? Description => "Local CodeCafe MAF infrastructure agent.";

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult<AgentSession>(new LocalMafAgentSession());
    }

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var localSession = (LocalMafAgentSession)session;
        var serializedSession = JsonSerializer.SerializeToElement(
            new LocalMafAgentSessionState(
                localSession.TurnCount,
                localSession.StateBag.Serialize()),
            jsonSerializerOptions);

        return ValueTask.FromResult(serializedSession);
    }

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var state = serializedState.Deserialize<LocalMafAgentSessionState>(jsonSerializerOptions)
            ?? new LocalMafAgentSessionState();

        return ValueTask.FromResult<AgentSession>(new LocalMafAgentSession(
            state.TurnCount,
            AgentSessionStateBag.Deserialize(state.StateBag)));
    }

    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var localSession = GetLocalSession(session);
        localSession.TurnCount++;
        var input = GetLatestUserText(messages);
        var text = CreateResponseText(input, localSession.TurnCount);

        return Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, text)));
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await RunCoreAsync(messages, session, options, cancellationToken);

        yield return new AgentResponseUpdate(ChatRole.Assistant, response.Text);
    }

    private static LocalMafAgentSession GetLocalSession(AgentSession? session)
    {
        return session as LocalMafAgentSession
            ?? throw new InvalidOperationException("The MAF session is not compatible with the local CodeCafe agent.");
    }

    private static string GetLatestUserText(IEnumerable<ChatMessage> messages)
    {
        return messages
            .LastOrDefault(message => message.Role == ChatRole.User)
            ?.Text
            ?? string.Empty;
    }

    private string CreateResponseText(string input, int turnCount)
    {
        return string.IsNullOrWhiteSpace(input)
            ? $"CodeCafe MAF profile '{profileId}' is ready. Turn {turnCount}."
            : $"CodeCafe MAF profile '{profileId}' received: {input}";
    }

    private sealed class LocalMafAgentSession(
        int turnCount = 0,
        AgentSessionStateBag? stateBag = null) : AgentSession(stateBag ?? new AgentSessionStateBag())
    {
        public int TurnCount { get; set; } = turnCount;
    }

    private sealed record LocalMafAgentSessionState(
        int TurnCount = 0,
        JsonElement StateBag = default);
}
