using System.Text.Json;
using CodeCafe.Application.Ai;
using CodeCafe.Application.Ai.StartAiChat;
using CodeCafe.Application.Blocks.ApplyBlockOps;
using CodeCafe.Application.Common;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.GetPage;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Domain.Notebooks;
using Microsoft.Extensions.AI;

namespace CodeCafe.Application.Tests.Ai;

public sealed class AssistantToolsTests
{
    private static readonly AiOptions Options = new() { MaxToolResultChars = 200 };

    [Fact]
    public async Task Tool_Success_ReturnsCappedJson()
    {
        var details = DetailsDto(title: new string('x', 500));
        var sender = new StubSender().Respond<GetPageQuery>(Result.Success(details));
        var tool = Find(AssistantTools.Create(sender, NewNotebook(), Options), "get_page");

        var result = await Invoke(tool, new { pageId = Guid.NewGuid() });

        Assert.True(result!.Length <= Options.MaxToolResultChars + "\n…(truncated)".Length);
        Assert.EndsWith("…(truncated)", result);
    }

    [Fact]
    public async Task Tool_HandlerFailure_ReturnsErrorJson()
    {
        var sender = new StubSender().Respond<GetPageQuery>(Result.Failure<PageDetailsDto>(PageErrors.NotFound));
        var tool = Find(AssistantTools.Create(sender, NewNotebook(), Options), "get_page");

        var result = await Invoke(tool, new { pageId = Guid.NewGuid() });

        using var json = JsonDocument.Parse(result!);
        var error = json.RootElement.GetProperty("error");
        Assert.Equal("page_not_found", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ApplyBlockOps_DeserializesOpsAndPassesDryRun()
    {
        var sender = new StubSender().Respond<ApplyBlockOpsCommand>(
            Result.Success<IReadOnlyList<BlockOpResultDto>>([])
        );
        var tool = Find(AssistantTools.Create(sender, NewNotebook(), Options), "apply_block_ops");
        var pageId = Guid.NewGuid();
        var ops = JsonSerializer.SerializeToElement(
            new object[]
            {
                new
                {
                    kind = "insert",
                    tempId = "a1",
                    type = "paragraph",
                    content = new { spans = new[] { new { text = "hi", marks = Array.Empty<object>() } } },
                    after = (string?)null,
                },
            }
        );

        await Invoke(tool, new { pageId, ops, dryRun = true });

        var command = sender.LastOfType<ApplyBlockOpsCommand>()!;
        Assert.Equal(pageId, command.PageId);
        Assert.True(command.DryRun);
        var op = Assert.Single(command.Ops);
        Assert.Equal(BlockOpKind.Insert, op.Kind);
        Assert.Equal("a1", op.TempId);
    }

    [Fact]
    public void Create_ExcludesUnrecoverableSharingAndRecursiveTools()
    {
        var tools = AssistantTools.Create(new StubSender(), NewNotebook(), Options);

        Assert.DoesNotContain(
            tools,
            tool => tool.Name is "purge_notebook" or "purge_trashed_notebooks" or "purge_page"
                or "share_notebook" or "share_page" or "revoke_notebook_share" or "revoke_page_share"
                or "set_notebook_access_code" or "ai_chat"
        );
    }

    [Fact]
    public void Create_KeepsTheRecoverableCounterparts()
    {
        var tools = AssistantTools.Create(new StubSender(), NewNotebook(), Options);

        Assert.Contains(tools, tool => tool.Name == "delete_notebook");
        Assert.Contains(tools, tool => tool.Name == "restore_notebook");
        Assert.Contains(tools, tool => tool.Name == "delete_page");
        Assert.Contains(tools, tool => tool.Name == "restore_page");
    }

    [Fact]
    public void Create_ToolNamesAreUniqueAndDescribed()
    {
        var tools = AssistantTools.Create(new StubSender(), NewNotebook(), Options);

        Assert.Equal(tools.Count, tools.Select(tool => tool.Name).Distinct().Count());
        Assert.All(tools, tool => Assert.False(string.IsNullOrWhiteSpace(tool.Description)));
    }

    [Fact]
    public void Create_AdvertisesTempIdOnlyWhereItWorks()
    {
        var tools = AssistantTools.Create(new StubSender(), NewNotebook(), Options);

        // insert_blocks takes a flat run at one position, so BlockInput carries no tempId and the
        // model must not be told otherwise. Batch-relative references are apply_block_ops' job.
        Assert.DoesNotContain("tempId", Find(tools, "insert_blocks").Description);
        Assert.Contains("tempId", Find(tools, "apply_block_ops").Description);
    }

    private static AIFunction Find(IReadOnlyList<AIFunction> tools, string name)
        => tools.Single(tool => tool.Name == name);

    private static Notebook NewNotebook()
        => Notebook.Create(Guid.NewGuid(), "My Notes", null, "nb", NotebookVisibility.Private);

    private static PageDetailsDto DetailsDto(string title) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            title,
            "/p",
            false,
            false,
            [],
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            CanWrite: false
        );

    private static async Task<string?> Invoke(AIFunction tool, object arguments)
    {
        var args = JsonSerializer
            .SerializeToElement(arguments)
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => (object?)property.Value);
        var result = await tool.InvokeAsync(new AIFunctionArguments(args));
        return result switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            _ => JsonSerializer.Serialize(result),
        };
    }
}
