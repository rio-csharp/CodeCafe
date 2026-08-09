using CodeCafe.Application.Common.Uploads;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Common.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CodeCafe.Host.Mcp;

/// <remarks>
/// These tools serve unauthenticated callers, so their output bounds matter more than the
/// authenticated ones': an uncapped list limit or a notebook of full-text pages lets one call pull an
/// unbounded response. Both are clamped with the same McpOptions budgets the authenticated read tools
/// use.
/// </remarks>
[McpServerToolType]
public sealed class NotesReadMcpTools
{
    private const int DefaultPublicListLimit = 25;

    /// <summary>
    /// Per-item plain-text budget for the public notebook detail response. Full page content is
    /// available through the authenticated tools; the public projection only needs enough text to
    /// identify a page.
    /// </summary>
    private const int PublicItemPreviewChars = 2000;
    [McpServerTool(
        Name = "notes_list_public_notebooks",
        Title = "List Public Notebooks",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(ListPublicNotebooksResponse))]
    [Description("List public notebooks exposed through this MCP endpoint.")]
    public async Task<CallToolResult> ListPublicNotebooksAsync(
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional search query.")] string? query = null,
        [Description("Maximum number of notebooks to return.")] int? limit = null)
    {
        // Previously passed straight through, so a caller could ask for every public notebook.
        var maxResults = Math.Clamp(
            limit ?? DefaultPublicListLimit,
            1,
            mcpOptionsAccessor.Value.MaxListItemsLimit);
        var notebooks = await notebookReadService.GetPublicNotebooksAsync(
            query,
            Guid.Empty,
            cancellationToken,
            maxResults);

        var response = new ListPublicNotebooksResponse(
            notebooks.Count,
            notebooks.Select(notebook => new PublicNotebookSummary(
                notebook.Id,
                notebook.Title,
                notebook.Slug,
                notebook.Description,
                notebook.AuthorDisplayName,
                notebook.ItemCount,
                notebook.FolderCount,
                notebook.PageCount,
                notebook.PublishedAtUtc)).ToList());

        return NotesMcpResultMapper.Success(response, $"Listed {response.TotalCount} public notebook(s).");
    }

    [McpServerTool(
        Name = "notes_get_public_notebook",
        Title = "Get Public Notebook",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(PublicNotebookDetailResponse))]
    [Description("Load one public notebook by slug, including its visible items.")]
    public async Task<CallToolResult> GetPublicNotebookAsync(
        [Description("The notebook slug.")] string slug,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken)
    {
        var result = await notebookReadService.GetPublicNotebookAsync(
            slug,
            Guid.Empty,
            cancellationToken);

        if (!result.Succeeded)
        {
            return NotesMcpResultMapper.Failure(result.Error!);
        }

        var notebook = result.Value!;
        var response = new PublicNotebookDetailResponse(
            notebook.Id,
            notebook.Title,
            notebook.Slug,
            notebook.Description,
            notebook.AuthorDisplayName,
            notebook.ItemCount,
            notebook.FolderCount,
            notebook.PageCount,
            notebook.PublishedAtUtc,
            notebook.Items.Select(item => new PublicNotebookItemSummary(
                item.Id,
                item.Type,
                item.Title,
                item.Path,
                item.SortOrder,
                TruncatePreview(item.PlainTextContent))).ToList());

        return NotesMcpResultMapper.Success(response, $"Loaded public notebook '{response.Title}'.");
    }

    private static string? TruncatePreview(string? value)
    {
        if (value is null || value.Length <= PublicItemPreviewChars)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, PublicItemPreviewChars), "…[truncated]");
    }
}

public sealed record ListPublicNotebooksResponse(
    int TotalCount,
    IReadOnlyList<PublicNotebookSummary> Notebooks);

public sealed record PublicNotebookSummary(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string AuthorDisplayName,
    int ItemCount,
    int FolderCount,
    int PageCount,
    DateTimeOffset? PublishedAtUtc);

public sealed record PublicNotebookDetailResponse(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string AuthorDisplayName,
    int ItemCount,
    int FolderCount,
    int PageCount,
    DateTimeOffset? PublishedAtUtc,
    IReadOnlyList<PublicNotebookItemSummary> Items);

public sealed record PublicNotebookItemSummary(
    Guid Id,
    string Type,
    string Title,
    string Path,
    int SortOrder,
    string? PlainTextContent);
