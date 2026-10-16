using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCafe.Application.Blocks.ApplyBlockOps;
using CodeCafe.Application.Blocks.DeleteBlock;
using CodeCafe.Application.Blocks.InsertBlocks;
using CodeCafe.Application.Blocks.MoveBlock;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.UpdateBlock;
using CodeCafe.Application.Common;
using CodeCafe.Application.Notebooks.ChangeNotebookSlug;
using CodeCafe.Application.Notebooks.CreateNotebook;
using CodeCafe.Application.Notebooks.DeleteNotebook;
using CodeCafe.Application.Notebooks.ExportNotebook;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Application.Notebooks.GetNotebookTree;
using CodeCafe.Application.Notebooks.ImportNotebook;
using CodeCafe.Application.Notebooks.ListNotebooks;
using CodeCafe.Application.Notebooks.SetNotebookFavorite;
using CodeCafe.Application.Notebooks.SetNotebookTags;
using CodeCafe.Application.Notebooks.UpdateNotebook;
using CodeCafe.Application.Pages.CreatePage;
using CodeCafe.Application.Pages.DeletePage;
using CodeCafe.Application.Pages.ExportPage;
using CodeCafe.Application.Pages.GetPage;
using CodeCafe.Application.Pages.GetPageByPath;
using CodeCafe.Application.Pages.ImportPage;
using CodeCafe.Application.Pages.ListFavoritePages;
using CodeCafe.Application.Pages.MovePage;
using CodeCafe.Application.Pages.SearchAllPages;
using CodeCafe.Application.Pages.SetPageFavorite;
using CodeCafe.Application.Pages.UpdatePage;
using CodeCafe.Application.Revisions.ListBlockRevisions;
using CodeCafe.Application.Revisions.ListPageRevisions;
using CodeCafe.Application.Revisions.RestoreBlockRevision;
using CodeCafe.Application.Revisions.RestorePageToRevision;
using CodeCafe.Application.Trash.ListTrash;
using CodeCafe.Application.Trash.ListTrashedPages;
using CodeCafe.Application.Trash.RestoreNotebookFromTrash;
using CodeCafe.Application.Trash.RestorePageFromTrash;
using CodeCafe.Domain.Notebooks;
using MediatR;
using Microsoft.Extensions.AI;

namespace CodeCafe.Application.Ai.StartAiChat;

// The assistant's tool surface. Each tool delegates to the real handler, so permission checks and
// Ai revision attribution apply exactly as for a human caller.
//
// Three capabilities are deliberately absent, and the omission is the enforcement:
//   - permanent deletion (purge_*): unrecoverable, and the UI's confirmation dialog has no
//     equivalent here;
//   - sharing and access codes (share_*, revoke_*_share, set_notebook_access_code): these change
//     who else can see the data, so a page carrying a prompt injection would gain a path to
//     exfiltrate the notebook;
//   - ai_chat: the assistant calling itself recurses without bound.
// Trashing (delete_page / delete_notebook) stays available -- it is recoverable, and the matching
// restore_* tools are here too.
public static class AssistantTools
{
    private const string TruncatedMarker = "\n…(truncated)";

    // Matches the JSON shape the REST API speaks, so the model can reuse API documentation.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static IReadOnlyList<AIFunction> Create(ISender sender, Notebook notebook, AiOptions options)
    {
        var notebookIdOrSlug = notebook.Slug;

        return
        [
            // --- Notebooks ---------------------------------------------------------------
            AIFunctionFactory.Create(
                async (CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new GetNotebookDetailsQuery(notebookIdOrSlug), cancellationToken), options),
                "get_notebook",
                "Get this notebook's details (title, description, slug, visibility, tags, favorite)."
            ),
            AIFunctionFactory.Create(
                async (string? search, string? tag, bool? favorite, int? page, CancellationToken cancellationToken) =>
                    ToolResult(
                        await sender.Send(new ListNotebooksQuery(tag, favorite, null, search, null, page, null), cancellationToken),
                        options
                    ),
                "list_notebooks",
                "List notebooks visible to the current user, with optional free-text search, tag and favorite filters."
            ),
            AIFunctionFactory.Create(
                async (string title, string? description, string? slug, NotebookVisibility visibility, CancellationToken cancellationToken) =>
                    ToolResult(
                        await sender.Send(new CreateNotebookCommand(title, description, slug, visibility), cancellationToken),
                        options
                    ),
                "create_notebook",
                "Create a new notebook, separate from the one being discussed."
            ),
            AIFunctionFactory.Create(
                async (string? title, string? description, NotebookVisibility? visibility, CancellationToken cancellationToken) =>
                    ToolResult(
                        await sender.Send(new UpdateNotebookCommand(notebookIdOrSlug, title, description, visibility), cancellationToken),
                        options
                    ),
                "update_notebook",
                "Update this notebook's title, description or visibility."
            ),
            AIFunctionFactory.Create(
                async (string slug, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new ChangeNotebookSlugCommand(notebookIdOrSlug, slug), cancellationToken), options),
                "change_notebook_slug",
                "Change this notebook's URL slug. Existing links to the old slug stop working."
            ),
            AIFunctionFactory.Create(
                async (bool isFavorite, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new SetNotebookFavoriteCommand(notebookIdOrSlug, isFavorite), cancellationToken), options),
                "set_notebook_favorite",
                "Mark or unmark this notebook as favorite."
            ),
            AIFunctionFactory.Create(
                async (IReadOnlyList<string> tags, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new SetNotebookTagsCommand(notebookIdOrSlug, tags), cancellationToken), options),
                "set_notebook_tags",
                "Replace all tags of this notebook."
            ),
            AIFunctionFactory.Create(
                async (CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new ExportNotebookQuery(notebookIdOrSlug), cancellationToken), options),
                "export_notebook",
                "Export this whole notebook as markdown: a JSON object with fileName and markdown."
            ),
            AIFunctionFactory.Create(
                async (string fileName, string markdown, CancellationToken cancellationToken) =>
                    ToolResult(
                        await sender.Send(new ImportNotebookCommand(new NotebookExportDto(fileName, markdown)), cancellationToken),
                        options
                    ),
                "import_notebook",
                "Create a new notebook from a markdown payload (the shape export_notebook returns)."
            ),
            AIFunctionFactory.Create(
                async (CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new DeleteNotebookCommand(notebookIdOrSlug), cancellationToken), options),
                "delete_notebook",
                "Move this notebook to the trash. Recover it with restore_notebook."
            ),
            AIFunctionFactory.Create(
                async (int? page, int? pageSize, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new ListTrashQuery(page, pageSize), cancellationToken), options),
                "list_trash",
                "List the notebooks currently in the trash."
            ),
            AIFunctionFactory.Create(
                async (Guid notebookId, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new RestoreNotebookFromTrashCommand(notebookId), cancellationToken), options),
                "restore_notebook",
                "Restore a trashed notebook by id (from list_trash)."
            ),

            // --- Pages -------------------------------------------------------------------
            AIFunctionFactory.Create(
                async (CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new GetNotebookTreeQuery(notebookIdOrSlug), cancellationToken), options),
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
                async (Guid pageId, string? title, bool? isArchived, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new UpdatePageCommand(pageId, title, isArchived), cancellationToken), options),
                "update_page",
                "Rename a page and/or change its archived state. Archived pages drop out of the reader tree."
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
                async (Guid pageId, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new DeletePageCommand(pageId), cancellationToken), options),
                "delete_page",
                "Move a page and its subtree to the trash. Recover it with restore_page."
            ),
            AIFunctionFactory.Create(
                async (Guid pageId, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new ExportPageQuery(pageId), cancellationToken), options),
                "export_page",
                "Export one page with all its blocks as markdown: a JSON object with fileName and markdown."
            ),
            AIFunctionFactory.Create(
                async (string fileName, string markdown, string? parentPath, CancellationToken cancellationToken) =>
                    ToolResult(
                        await sender.Send(
                            new ImportPageCommand(notebookIdOrSlug, new PageExportDto(fileName, markdown), parentPath),
                            cancellationToken
                        ),
                        options
                    ),
                "import_page",
                "Create a page in this notebook from a markdown payload, optionally under a parent page path."
            ),
            AIFunctionFactory.Create(
                async (Guid pageId, bool isFavorite, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new SetPageFavoriteCommand(pageId, isFavorite), cancellationToken), options),
                "set_page_favorite",
                "Mark or unmark a page as favorite."
            ),
            AIFunctionFactory.Create(
                async (CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new ListFavoritePagesQuery(notebook.Id), cancellationToken), options),
                "list_favorite_pages",
                "List the pages in this notebook that the current user has marked as favorite."
            ),
            AIFunctionFactory.Create(
                async (int? page, int? pageSize, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new ListTrashedPagesQuery(notebookIdOrSlug, page, pageSize), cancellationToken), options),
                "list_trashed_pages",
                "List the pages currently in this notebook's trash."
            ),
            AIFunctionFactory.Create(
                async (Guid pageId, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new RestorePageFromTrashCommand(pageId), cancellationToken), options),
                "restore_page",
                "Restore a trashed page and its subtree."
            ),

            // --- Blocks ------------------------------------------------------------------
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
            AIFunctionFactory.Create(
                async (Guid pageId, JsonElement blocks, Guid? afterBlockId, CancellationToken cancellationToken) =>
                    ToolResult(
                        await sender.Send(
                            new InsertBlocksCommand(pageId, afterBlockId, blocks.Deserialize<List<BlockInput>>(JsonOptions) ?? []),
                            cancellationToken
                        ),
                        options
                    ),
                "insert_blocks",
                "Insert new blocks (each {type, content}) into a page after the given block; "
                    + "afterBlockId=null inserts at the start of the page. Use apply_block_ops when the insert "
                    + "has to land relative to other edits in the same call."
            ),
            AIFunctionFactory.Create(
                async (Guid pageId, Guid blockId, JsonElement content, long baseVersion, CancellationToken cancellationToken) =>
                    ToolResult(
                        await sender.Send(new UpdateBlockCommand(pageId, blockId, content, baseVersion), cancellationToken),
                        options
                    ),
                "update_block",
                "Replace the content of one block. Requires baseVersion: the block's current version from get_page."
            ),
            AIFunctionFactory.Create(
                async (Guid pageId, Guid blockId, Guid? afterBlockId, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new MoveBlockCommand(pageId, blockId, afterBlockId), cancellationToken), options),
                "move_block",
                "Move a block so it sits after the given block (null = start of the page)."
            ),
            AIFunctionFactory.Create(
                async (Guid pageId, Guid blockId, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new DeleteBlockCommand(pageId, blockId), cancellationToken), options),
                "delete_block",
                "Delete a block from a page."
            ),

            // --- Revisions ---------------------------------------------------------------
            AIFunctionFactory.Create(
                async (Guid pageId, string? cursor, int? pageSize, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new ListPageRevisionsQuery(pageId, cursor, pageSize), cancellationToken), options),
                "list_page_revisions",
                "List the revision history of a page, newest first. Use it to find a timestamp to restore."
            ),
            AIFunctionFactory.Create(
                async (Guid pageId, Guid blockId, string? cursor, int? pageSize, CancellationToken cancellationToken) =>
                    ToolResult(
                        await sender.Send(new ListBlockRevisionsQuery(pageId, blockId, cursor, pageSize), cancellationToken),
                        options
                    ),
                "list_block_revisions",
                "List the revision history of one block, newest first. Use it to find a version to restore."
            ),
            AIFunctionFactory.Create(
                async (Guid pageId, DateTimeOffset atUtc, CancellationToken cancellationToken) =>
                    ToolResult(await sender.Send(new RestorePageToRevisionCommand(pageId, atUtc), cancellationToken), options),
                "restore_page_revision",
                "Roll a whole page back to its state at the given point in time. The rollback is recorded as a "
                    + "revision itself, so it can be undone the same way."
            ),
            AIFunctionFactory.Create(
                async (Guid pageId, Guid blockId, long blockVersion, CancellationToken cancellationToken) =>
                    ToolResult(
                        await sender.Send(new RestoreBlockRevisionCommand(pageId, blockId, blockVersion), cancellationToken),
                        options
                    ),
                "restore_block_revision",
                "Roll one block back to a specific block version (from list_block_revisions)."
            ),
        ];
    }

    private static string ToolResult(Result result, AiOptions options)
        => Truncate(result.IsSuccess ? "Done." : ErrorJson(result.Error!), options);

    private static string ToolResult<T>(Result<T> result, AiOptions options)
        => Truncate(result.IsSuccess ? Serialize(result.Value) : ErrorJson(result.Error!), options);

    private static string Serialize<T>(T? value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string ErrorJson(Error error)
        => JsonSerializer.Serialize(new { error = new { error.Code, error.Message } }, JsonOptions);

    private static string Truncate(string json, AiOptions options)
        => json.Length <= options.MaxToolResultChars
            ? json
            : string.Concat(json.AsSpan(0, options.MaxToolResultChars), TruncatedMarker);
}
