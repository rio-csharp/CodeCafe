using CodeCafe.Application.Ai;
using CodeCafe.Application.Ai.StartAiChat;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.GetNotebookTree;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Revisions;
using Microsoft.Extensions.AI;

namespace CodeCafe.Application.Tests.Ai;

public sealed class StartAiChatCommandHandlerTests
{
    private static readonly AiOptions EnabledOptions = new()
    {
        Enabled = true,
        Model = "test-model",
        ApiKey = "test-key",
    };

    [Fact]
    public async Task Handle_Disabled_EmitsErrorEvent()
    {
        var (handler, _, _, _) = CreateHandler(new AiOptions { Enabled = false });

        var events = await Collect(handler, Chat());

        var error = Assert.Single(events);
        Assert.Equal(AiChatEventKinds.Error, error.Kind);
        Assert.Equal(AiErrors.Disabled.Code, PayloadCode(error));
    }

    [Fact]
    public async Task Handle_EnabledButNotConfigured_EmitsErrorEvent()
    {
        var (handler, _, _, _) = CreateHandler(new AiOptions { Enabled = true });

        var events = await Collect(handler, Chat());

        var error = Assert.Single(events);
        Assert.Equal(AiErrors.NotConfigured.Code, PayloadCode(error));
    }

    [Fact]
    public async Task Handle_AnonymousUser_EmitsNotebookNotFound()
    {
        var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        var notebook = SeedNotebook(owner);
        var (handler, _, _, _) = CreateHandler(EnabledOptions, notebook, currentUserId: null);

        var events = await Collect(handler, Chat());

        AssertError(events, "notebook_not_found");
    }

    [Fact]
    public async Task Handle_StrangerCannotRead_EmitsNotebookNotFound()
    {
        var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var (handler, _, _, _) = CreateHandler(EnabledOptions, notebook, stranger.Id);

        var events = await Collect(handler, Chat());

        AssertError(events, "notebook_not_found");
    }

    [Fact]
    public async Task Handle_HistoryTooLong_EmitsErrorEvent()
    {
        var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        var notebook = SeedNotebook(owner);
        var options = new AiOptions { Enabled = true, Model = "m", ApiKey = "k", MaxHistoryChars = 10 };
        var (handler, _, _, _) = CreateHandler(options, notebook, owner.Id);

        var events = await Collect(handler, Chat("this is way over the limit"));

        AssertError(events, AiErrors.HistoryTooLong.Code);
    }

    [Fact]
    public async Task Handle_BuildsStablePrefix_SystemThenOutlineThenHistory()
    {
        var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        var notebook = SeedNotebook(owner);
        var (handler, chatClient, sender, _) = CreateHandler(EnabledOptions, notebook, owner.Id);
        chatClient.Enqueue(new ChatResponseUpdate(ChatRole.Assistant, "hello"));

        var events = await Collect(
            handler,
            new StartAiChatCommand(
                notebook.Slug,
                [new AiChatMessage(AiChatRole.User, "q1"), new AiChatMessage(AiChatRole.Assistant, "a1"), new AiChatMessage(AiChatRole.User, "q2")]
            )
        );

        Assert.Equal([AiChatEventKinds.Text, AiChatEventKinds.Done], events.Select(e => e.Kind));
        Assert.Equal("hello", events[0].Message);

        var received = Assert.Single(chatClient.ReceivedMessages);
        Assert.Equal(ChatRole.System, received[0].Role);
        Assert.Contains(notebook.Title, received[0].Text);
        Assert.Contains("/guide", received[0].Text); // outline from the canned tree
        Assert.Equal([ChatRole.User, ChatRole.Assistant, ChatRole.User], received.Skip(1).Select(m => m.Role));
        Assert.True(chatClient.ReceivedOptions[0]!.Tools!.Count > 0);
        _ = sender; // tree fetched through the same sender, asserted in the tool-loop test
    }

    [Fact]
    public async Task Handle_SetsChangeSourceToAi()
    {
        var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        var notebook = SeedNotebook(owner);
        var (handler, chatClient, _, changeSource) = CreateHandler(EnabledOptions, notebook, owner.Id);
        chatClient.Enqueue(new ChatResponseUpdate(ChatRole.Assistant, "hi"));

        await Collect(handler, Chat());

        Assert.Equal(RevisionSource.Ai, changeSource.Source);
    }

    [Fact]
    public async Task Handle_ToolCall_ExecutesToolAndEmitsToolEvents()
    {
        var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        var notebook = SeedNotebook(owner);
        var (handler, chatClient, sender, _) = CreateHandler(EnabledOptions, notebook, owner.Id);
        chatClient.Enqueue(
            new ChatResponseUpdate
            {
                Contents = { new FunctionCallContent("call-1", "get_notebook_tree", new Dictionary<string, object?>()) },
            }
        );
        chatClient.Enqueue(new ChatResponseUpdate(ChatRole.Assistant, "you have 1 page"));

        var events = await Collect(handler, Chat());

        Assert.Equal(
            [AiChatEventKinds.ToolCall, AiChatEventKinds.ToolResult, AiChatEventKinds.Text, AiChatEventKinds.Done],
            events.Select(e => e.Kind)
        );
        Assert.Equal("get_notebook_tree", events[0].Tool);
        Assert.Equal(2, sender.Received.OfType<GetNotebookTreeQuery>().Count()); // outline + tool call
    }

    [Fact]
    public async Task Handle_ProviderThrowsMidStream_EmitsErrorEvent()
    {
        var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        var notebook = SeedNotebook(owner);
        var (handler, chatClient, _, _) = CreateHandler(EnabledOptions, notebook, owner.Id);
        chatClient.ExceptionToThrow = new HttpRequestException("relay is down");

        var events = await Collect(handler, Chat());

        var error = Assert.Single(events);
        Assert.Equal(AiChatEventKinds.Error, error.Kind);
        Assert.Equal(AiErrors.ProviderFailed.Code, PayloadCode(error));
    }

    private static StartAiChatCommand Chat(string message = "hello")
        => new("my-notebook", [new AiChatMessage(AiChatRole.User, message)]);

    private static void AssertError(List<AiChatEvent> events, string code)
    {
        var error = Assert.Single(events);
        Assert.Equal(AiChatEventKinds.Error, error.Kind);
        Assert.Equal(code, PayloadCode(error));
    }

    private static string? PayloadCode(AiChatEvent aiEvent)
        => aiEvent.Payload!.Value.GetProperty("code").GetString();

    private static async Task<List<AiChatEvent>> Collect(StartAiChatCommandHandler handler, StartAiChatCommand command)
    {
        var events = new List<AiChatEvent>();
        await foreach (var aiEvent in handler.Handle(command, CancellationToken.None))
        {
            events.Add(aiEvent);
        }

        return events;
    }

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "My Notes", null, "my-notebook", NotebookVisibility.Private);

    private static (
        StartAiChatCommandHandler Handler,
        StubChatClient ChatClient,
        StubSender Sender,
        StubChangeSourceAccessor ChangeSource
    ) CreateHandler(AiOptions options, Notebook? notebook = null, Guid? currentUserId = null)
    {
        var chatClient = new StubChatClient();
        var sender = new StubSender().Respond<GetNotebookTreeQuery>(
            Result.Success(
                new NotebookTreeDto(
                    notebook?.Id ?? Guid.NewGuid(),
                    [
                        new PageTreeNodeDto(
                            Guid.NewGuid(),
                            "Guide",
                            "/guide",
                            0,
                            false,
                            false,
                            [new PageTreeNodeDto(Guid.NewGuid(), "Setup", "/guide/setup", 0, false, false, [])]
                        ),
                    ]
                )
            )
        );
        var notebooks = new StubNotebookRepository();
        if (notebook is not null)
        {
            notebooks.Add(notebook);
        }

        var changeSource = new StubChangeSourceAccessor();
        return (
            new StartAiChatCommandHandler(
                new StubCurrentUserAccessor(currentUserId is { } id ? new CurrentUser(id) : null),
                notebooks,
                new StubPasswordHasher(),
                sender,
                new StubAiChatClientFactory(chatClient),
                changeSource,
                options
            ),
            chatClient,
            sender,
            changeSource
        );
    }

    private sealed class StubAiChatClientFactory(StubChatClient client) : IAiChatClientFactory
    {
        public IChatClient Create() => client;
    }
}
