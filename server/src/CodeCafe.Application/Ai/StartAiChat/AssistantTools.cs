using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCafe.Application.Blocks.ApplyBlockOps;
using CodeCafe.Application.Common;
using CodeCafe.Application.Notebooks.GetNotebookTree;
using CodeCafe.Application.Pages.CreatePage;
using CodeCafe.Application.Pages.GetPage;
using CodeCafe.Application.Pages.GetPageByPath;
using CodeCafe.Application.Pages.MovePage;
using CodeCafe.Application.Pages.SearchAllPages;
using CodeCafe.Application.Pages.UpdatePage;
using MediatR;
using Microsoft.Extensions.AI;

namespace CodeCafe.Application.Ai.StartAiChat;

// The assistant's tool surface: a curated set of high-leverage tools, not a 1:1 mirror of every
// use case — tool schemas cost prompt tokens on every request. Each tool delegates to the real
// handler, so permission checks and Ai revision attribution apply exactly as for a human caller.
public static class AssistantTools
{
    private const string TruncatedMarker = "\n…(truncated)";

    // Matches the JSON shape the REST API speaks, so the model can reuse API documentation.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static IReadOnlyList<AIFunction> Create(ISender sender, string notebookIdOrSlug, AiOptions options) =>
    [
        AIFunctionFactory.Create(
            async (CancellationToken cancellationToken) =>
                ToolResult(
                    await sender.Send(new GetNotebookTreeQuery(notebookIdOrSlug), cancellationToken),
                    options
                ),
            "get_notebook_tree",
            "Get the notebook's page tree (ids, titles, paths, hierarchy) without page content."
        ),
        AIFunctionFactory.Create(
            async (string query, CancellationToken cancellationToken) =>
                ToolResult(await sender.Send(new SearchAllPagesQuery(query, null, 20), cancellationToken), options),
            "search_pages",
            "Full-text search pages across all notebooks the user can read. Returns matching pages with snippets."
        ),
        AIFunctionFactory.Create(
            async (Guid pageId, CancellationToken cancellationToken) =>
                ToolResult(await sender.Send(new GetPageQuery(pageId), cancellationToken), options),
            "get_page",
            "Get a page's details including ALL its blocks (ids, versions, types, payloads). Read before editing."
        ),
        AIFunctionFactory.Create(
            async (string path, CancellationToken cancellationToken) =>
                ToolResult(await sender.Send(new GetPageByPathQuery(notebookIdOrSlug, path), cancellationToken), options),
            "get_page_by_path",
            "Get a page by its slash path (e.g. /guide/setup), including all its blocks."
        ),
        AIFunctionFactory.Create(
            async (string title, string? parentPath, CancellationToken cancellationToken) =>
                ToolResult(
                    await sender.Send(new CreatePageCommand(notebookIdOrSlug, title, parentPath), cancellationToken),
                    options
                ),
            "create_page",
            "Create a page in this notebook, optionally under a parent page path. Returns the new page's id."
        ),
        AIFunctionFactory.Create(
            async (Guid pageId, string title, CancellationToken cancellationToken) =>
                ToolResult(await sender.Send(new UpdatePageCommand(pageId, title, null), cancellationToken), options),
            "rename_page",
            "Rename a page."
        ),
        AIFunctionFactory.Create(
            async (Guid pageId, string? newParentPath, Guid? afterPageId, CancellationToken cancellationToken) =>
                ToolResult(
                    await sender.Send(new MovePageCommand(pageId, newParentPath, afterPageId), cancellationToken),
                    options
                ),
            "move_page",
            "Move a page to a new parent path (null = notebook root), optionally ordered after a sibling page."
        ),
        AIFunctionFactory.Create(
            async (Guid pageId, JsonElement ops, bool dryRun, CancellationToken cancellationToken) =>
                ToolResult(
                    await sender.Send(
                        new ApplyBlockOpsCommand(pageId, ops.Deserialize<List<BlockOp>>(JsonOptions) ?? [], dryRun),
                        cancellationToken
                    ),
                    options
                ),
            "apply_block_ops",
            "Apply a batch of block operations to a page. Each op: {kind: 'insert'|'update'|'delete'|'move', "
                + "blockId?, tempId?, type?, after?, content?, baseVersion?}. insert: type + content payload, "
                + "tempId so later ops can reference it, after for placement. update: blockId + content + "
                + "baseVersion from get_page. delete/move: blockId. Set dryRun=true to preview without saving."
        ),
    ];

    private static string ToolResult<T>(Result<T> result, AiOptions options)
    {
        var json = result.IsSuccess
            ? JsonSerializer.Serialize(result.Value, JsonOptions)
            : JsonSerializer.Serialize(new { error = new { result.Error!.Code, result.Error.Message } }, JsonOptions);

        return json.Length <= options.MaxToolResultChars
            ? json
            : string.Concat(json.AsSpan(0, options.MaxToolResultChars), TruncatedMarker);
    }
}
