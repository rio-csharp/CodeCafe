using CodeCafe.Application.AI;
using CodeCafe.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.UnitTests.Infrastructure.AI.Maf;

public sealed class MafAgentRuntimeTests
{
    [Fact]
    public async Task RunAsync_uses_maf_agent_and_returns_serialized_session()
    {
        var runtime = CreateRuntime();

        var result = await runtime.RunAsync(
            new AgentRunRequest(
                "workspace-assistant",
                new AgentMessage(AgentMessageRole.User, "hello")),
            CancellationToken.None);

        Assert.Equal("workspace-assistant", result.ProfileId);
        Assert.Contains("hello", result.Text);
        Assert.False(string.IsNullOrWhiteSpace(result.Session.SerializedState));
    }

    [Fact]
    public async Task RunAsync_can_resume_from_serialized_session()
    {
        var runtime = CreateRuntime();
        var first = await runtime.RunAsync(
            new AgentRunRequest(
                "workspace-assistant",
                new AgentMessage(AgentMessageRole.User, "first")),
            CancellationToken.None);

        var second = await runtime.RunAsync(
            new AgentRunRequest(
                "workspace-assistant",
                new AgentMessage(AgentMessageRole.User, "second"),
                first.Session),
            CancellationToken.None);

        Assert.Contains("second", second.Text);
        Assert.NotEqual(first.Session.SerializedState, second.Session.SerializedState);
    }

    [Fact]
    public async Task RunStreamingAsync_returns_deltas_and_completed_snapshot()
    {
        var runtime = CreateRuntime();
        var updates = new List<AgentRunUpdate>();

        await foreach (var update in runtime.RunStreamingAsync(
            new AgentRunRequest(
                "workspace-assistant",
                new AgentMessage(AgentMessageRole.User, "stream me")),
            CancellationToken.None))
        {
            updates.Add(update);
        }

        Assert.Contains(updates, update => update.Kind == AgentRunUpdateKind.MessageDelta && update.Text?.Contains("stream me") == true);
        var completed = Assert.Single(updates, update => update.Kind == AgentRunUpdateKind.Completed);
        Assert.NotNull(completed.Session);
    }

    private static IAgentRuntime CreateRuntime()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:MiniMax:Provider"] = "Local",
            })
            .Build();
        services.AddCodeCafeInfrastructure(configuration);

        return services.BuildServiceProvider().GetRequiredService<IAgentRuntime>();
    }
}
