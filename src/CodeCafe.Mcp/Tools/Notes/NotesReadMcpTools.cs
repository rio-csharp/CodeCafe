using CodeCafe.Application.Notes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CodeCafe.Mcp.Tools.Notes;

[McpServerToolType]
public sealed class NotesReadMcpTools
{
    [McpServerTool(
        Name = "notes_list_public_notebooks",
        Title = "List Public Notebooks",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(ListPublicNotebooksResponse))]
    [Description("List public notebooks visible to anonymous or shared MCP readers.")]
    public async Task<CallToolResult> ListPublicNotebooksAsync(
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        [Description("Optional search query.")] string? query = null,
        [Description("Maximum number of notebooks to return.")] int? limit = null)
    {
        var notebooks = await notebookReadService.GetPublicNotebooksAsync(
            query,
            Guid.Empty,
            cancellationToken,
            limit);

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

        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock
                {
                    Text = $"Listed {response.TotalCount} public notebook(s)."
                }
            ],
            StructuredContent = System.Text.Json.JsonSerializer.SerializeToElement(response)
        };
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
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = result.Error!.Message
                    }
                ]
            };
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
                item.PlainTextContent)).ToList());

        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock
                {
                    Text = $"Loaded public notebook '{response.Title}'."
                }
            ],
            StructuredContent = System.Text.Json.JsonSerializer.SerializeToElement(response)
        };
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
