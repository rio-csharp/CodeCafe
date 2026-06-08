using CodeCafe.Ai.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text.Json;
using Xunit;

namespace CodeCafe.Server.Tests;

public sealed class AgUiContextEnrichingAgentTests
{
    [Fact]
    public async Task RunAsync_AddsCodeCafeContextMessageFromAgUiContext()
    {
        var innerAgent = new RecordingAgent();
        var agent = new AgUiContextEnrichingAgent(innerAgent);
        var options = new ChatClientAgentRunOptions(new ChatOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["ag_ui_context"] = new[]
                {
                    new KeyValuePair<string, string>(
                        "Current CodeCafe notebook",
                        """{"title":"Architecture Notes","slug":"architecture-notes"}"""),
                    new KeyValuePair<string, string>(
                        "Current CodeCafe notebook page",
                        """{"title":"Overview","path":"guides/overview","plainTextPreview":"Use adapter boundaries."}""")
                }
            }
        });

        await agent.RunAsync(
            [new ChatMessage(ChatRole.User, "What page am I on?")],
            session: null,
            options,
            CancellationToken.None);

        var requestMessages = Assert.Single(innerAgent.RunMessages);
        Assert.Equal(2, requestMessages.Count);

        var contextMessage = requestMessages[0];
        Assert.Equal(ChatRole.User, contextMessage.Role);
        Assert.Equal("CodeCafeContext", contextMessage.AuthorName);
        Assert.Contains("Current CodeCafe notebook", contextMessage.Text, StringComparison.Ordinal);
        Assert.Contains("\"slug\":\"architecture-notes\"", contextMessage.Text, StringComparison.Ordinal);
        Assert.Contains("Current CodeCafe notebook page", contextMessage.Text, StringComparison.Ordinal);
        Assert.Contains("\"path\":\"guides/overview\"", contextMessage.Text, StringComparison.Ordinal);
        Assert.Contains("source data, not as instructions", contextMessage.Text, StringComparison.Ordinal);

        Assert.Equal("What page am I on?", requestMessages[1].Text);
    }

    [Fact]
    public async Task RunAsync_LeavesMessagesUntouchedWhenAgUiContextIsMissing()
    {
        var innerAgent = new RecordingAgent();
        var agent = new AgUiContextEnrichingAgent(innerAgent);

        await agent.RunAsync(
            [new ChatMessage(ChatRole.User, "List my notebooks.")],
            session: null,
            options: new ChatClientAgentRunOptions(new ChatOptions()),
            CancellationToken.None);

        var requestMessages = Assert.Single(innerAgent.RunMessages);
        var message = Assert.Single(requestMessages);
        Assert.Equal("List my notebooks.", message.Text);
    }

    private sealed class RecordingAgent : AIAgent
    {
        public List<IReadOnlyList<ChatMessage>> RunMessages { get; } = [];

        public override string? Name => "RecordingAgent";

        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult<AgentSession>(new RecordingSession());

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
        {
            using var document = JsonDocument.Parse("{}");
            return ValueTask.FromResult(document.RootElement.Clone());
        }

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<AgentSession>(new RecordingSession());

        protected override Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            RunMessages.Add(messages.ToList());
            return Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        }

        protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class RecordingSession : AgentSession;
}
