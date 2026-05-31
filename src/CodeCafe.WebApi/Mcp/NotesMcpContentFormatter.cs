using System.Text;

namespace CodeCafe.WebApi.Mcp;

internal static class NotesMcpContentFormatter
{
    public static string Format<T>(T value, string fallbackSummary) where T : class
    {
        return value switch
        {
            ListNotebooksToolResponse response => FormatListNotebooks(response, fallbackSummary),
            GetNotebookToolResponse response => FormatGetNotebook(response, fallbackSummary),
            ListNotebookItemsToolResponse response => FormatListItems(response, fallbackSummary),
            GetPageToolResponse response => FormatGetPage(response, fallbackSummary),
            SearchNotesToolResponse response => FormatSearch(response, fallbackSummary),
            GetNotesLimitsToolResponse response => FormatLimits(response),
            CreateUploadToolResponse response => FormatCreateUpload(response),
            AppendUploadChunkToolResponse response => FormatAppendUploadChunk(response),
            DiscardUploadToolResponse response => FormatDiscardUpload(response),
            CreateItemToolResponse response => FormatCreateItem(response),
            CreatePageToolResponse response => FormatCreatePage(response),
            UpdatePageContentToolResponse response => FormatUpdatePageContent(response),
            MoveItemToolResponse response => FormatMoveItem(response),
            ReorderItemsToolResponse response => FormatReorderItems(response),
            DeleteItemToolResponse response => FormatDeleteItem(response),
            DeleteNotebookToolResponse response => FormatDeleteNotebook(response),
            _ => fallbackSummary
        };
    }

    private static string FormatListNotebooks(ListNotebooksToolResponse response, string fallbackSummary)
    {
        var builder = new StringBuilder();
        builder.AppendLine(fallbackSummary);
        builder.AppendLine();
        builder.AppendLine("Notebooks:");

        foreach (var notebook in response.Notebooks)
        {
            builder.AppendLine($"- {notebook.Title}");
            builder.AppendLine($"  slug: {notebook.Slug}");
            builder.AppendLine($"  visibility: {notebook.Visibility}");
            builder.AppendLine($"  canEdit: {notebook.CanEdit}");
            builder.AppendLine($"  notebookUri: {notebook.NotebookUri}");
            builder.AppendLine($"  itemsUri: {notebook.ItemsUri}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatGetNotebook(GetNotebookToolResponse response, string fallbackSummary)
    {
        var builder = new StringBuilder();
        builder.AppendLine(fallbackSummary);
        builder.AppendLine($"slug: {response.Slug}");
        builder.AppendLine($"visibility: {response.Visibility}");
        builder.AppendLine($"canEdit: {response.CanEdit}");
        builder.AppendLine($"itemCount: {response.ItemCount}");
        builder.AppendLine($"folderCount: {response.FolderCount}");
        builder.AppendLine($"pageCount: {response.PageCount}");
        builder.AppendLine($"notebookUri: {response.NotebookUri}");
        builder.AppendLine($"itemsUri: {response.ItemsUri}");

        if (!string.IsNullOrWhiteSpace(response.Description))
        {
            builder.AppendLine();
            builder.AppendLine("Description:");
            builder.AppendLine(response.Description);
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatListItems(ListNotebookItemsToolResponse response, string fallbackSummary)
    {
        var builder = new StringBuilder();
        builder.AppendLine(fallbackSummary);
        builder.AppendLine($"notebookSlug: {response.NotebookSlug}");
        builder.AppendLine($"canEdit: {response.CanEdit}");
        builder.AppendLine($"totalCount: {response.TotalCount}");
        builder.AppendLine($"offset: {response.Offset}");
        builder.AppendLine($"returnedCount: {response.ReturnedCount}");
        builder.AppendLine();
        builder.AppendLine("Items:");

        foreach (var item in response.Items)
        {
            builder.AppendLine($"- {item.Type}: {item.Title}");
            builder.AppendLine($"  path: {item.Path}");
            builder.AppendLine($"  slug: {item.Slug}");
            if (!string.IsNullOrWhiteSpace(item.ResourceUri))
            {
                builder.AppendLine($"  resourceUri: {item.ResourceUri}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatLimits(GetNotesLimitsToolResponse response)
    {
        var builder = new StringBuilder();
        builder.AppendLine("MCP limits loaded.");
        builder.AppendLine($"maxInlineContentBytes: {response.MaxInlineContentBytes}");
        builder.AppendLine($"maxUploadChunkBytes: {response.MaxUploadChunkBytes}");
        builder.AppendLine($"maxUploadBytes: {response.MaxUploadBytes}");
        builder.AppendLine($"maxPageContentBytes: {response.MaxPageContentBytes}");
        builder.AppendLine($"maxListItemsLimit: {response.MaxListItemsLimit}");
        builder.AppendLine($"supportedImportFormats: {string.Join(", ", response.SupportedImportFormats)}");
        return builder.ToString().TrimEnd();
    }

    private static string FormatCreateUpload(CreateUploadToolResponse response)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Upload '{response.UploadId}' created.");
        builder.AppendLine($"mediaType: {response.MediaType}");
        builder.AppendLine($"bytesReceived: {response.BytesReceived}");
        if (!string.IsNullOrWhiteSpace(response.FileName))
        {
            builder.AppendLine($"fileName: {response.FileName}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatAppendUploadChunk(AppendUploadChunkToolResponse response)
    {
        return $"Upload '{response.UploadId}' now holds {response.BytesReceived} bytes after appending {response.ChunkBytesReceived} bytes.";
    }

    private static string FormatDiscardUpload(DiscardUploadToolResponse response)
    {
        return $"Upload '{response.UploadId}' discarded.";
    }

    private static string FormatGetPage(GetPageToolResponse response, string fallbackSummary)
    {
        var builder = new StringBuilder();
        builder.AppendLine(fallbackSummary);
        builder.AppendLine($"notebookSlug: {response.NotebookSlug}");
        builder.AppendLine($"path: {response.Path}");
        builder.AppendLine($"canEdit: {response.CanEdit}");
        builder.AppendLine($"notebookUri: {response.NotebookUri}");
        builder.AppendLine($"pageUri: {response.PageUri}");

        if (!string.IsNullOrWhiteSpace(response.PlainTextContent))
        {
            builder.AppendLine();
            builder.AppendLine("Plain text content:");
            builder.AppendLine(response.PlainTextContent);
        }

        if (!string.IsNullOrWhiteSpace(response.ContentJson))
        {
            builder.AppendLine();
            builder.AppendLine("TipTap JSON:");
            builder.AppendLine("```json");
            builder.AppendLine(response.ContentJson);
            builder.AppendLine("```");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatSearch(SearchNotesToolResponse response, string fallbackSummary)
    {
        var builder = new StringBuilder();
        builder.AppendLine(fallbackSummary);
        builder.AppendLine();
        builder.AppendLine("Results:");

        foreach (var result in response.Results)
        {
            builder.AppendLine($"- {result.ResultType}: {result.ItemTitle ?? result.NotebookTitle}");
            builder.AppendLine($"  notebookSlug: {result.NotebookSlug}");
            builder.AppendLine($"  notebookUri: {result.NotebookUri}");
            if (!string.IsNullOrWhiteSpace(result.Path))
            {
                builder.AppendLine($"  path: {result.Path}");
            }
            if (!string.IsNullOrWhiteSpace(result.ResourceUri))
            {
                builder.AppendLine($"  resourceUri: {result.ResourceUri}");
            }
            if (!string.IsNullOrWhiteSpace(result.PlainTextSnippet))
            {
                builder.AppendLine($"  snippet: {result.PlainTextSnippet}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatCreateItem(CreateItemToolResponse response)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{Capitalize(response.Type)} '{response.Title}' created.");
        builder.AppendLine($"notebookSlug: {response.NotebookSlug}");
        builder.AppendLine($"path: {response.Path}");
        builder.AppendLine($"notebookUri: {response.NotebookUri}");
        builder.AppendLine($"itemsUri: {response.ItemsUri}");
        if (!string.IsNullOrWhiteSpace(response.ResourceUri))
        {
            builder.AppendLine($"resourceUri: {response.ResourceUri}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatCreatePage(CreatePageToolResponse response)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Page '{response.Title}' created.");
        builder.AppendLine($"notebookSlug: {response.NotebookSlug}");
        builder.AppendLine($"path: {response.Path}");
        builder.AppendLine($"notebookUri: {response.NotebookUri}");
        builder.AppendLine($"pageUri: {response.PageUri}");
        if (!string.IsNullOrWhiteSpace(response.PlainTextContent))
        {
            builder.AppendLine($"plainTextContent: {response.PlainTextContent}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatUpdatePageContent(UpdatePageContentToolResponse response)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Page '{response.Title}' updated.");
        builder.AppendLine($"notebookSlug: {response.NotebookSlug}");
        builder.AppendLine($"path: {response.Path}");
        builder.AppendLine($"notebookUri: {response.NotebookUri}");
        builder.AppendLine($"pageUri: {response.PageUri}");
        if (!string.IsNullOrWhiteSpace(response.PlainTextContent))
        {
            builder.AppendLine();
            builder.AppendLine("Updated plain text content:");
            builder.AppendLine(response.PlainTextContent);
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatMoveItem(MoveItemToolResponse response)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{Capitalize(response.Type)} '{response.Title}' moved.");
        builder.AppendLine($"notebookSlug: {response.NotebookSlug}");
        builder.AppendLine($"path: {response.Path}");
        builder.AppendLine($"notebookUri: {response.NotebookUri}");
        builder.AppendLine($"itemsUri: {response.ItemsUri}");
        if (!string.IsNullOrWhiteSpace(response.ResourceUri))
        {
            builder.AppendLine($"resourceUri: {response.ResourceUri}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatReorderItems(ReorderItemsToolResponse response)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Reordered {response.Items.Count} item(s) in notebook '{response.NotebookSlug}'.");
        builder.AppendLine("Items:");
        foreach (var item in response.Items)
        {
            builder.AppendLine($"- {item.Type}: {item.Path} (sortOrder: {item.SortOrder})");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatDeleteItem(DeleteItemToolResponse response)
        => $"Deleted item '{response.Path}' from notebook '{response.NotebookSlug}'.";

    private static string FormatDeleteNotebook(DeleteNotebookToolResponse response)
        => $"Deleted notebook '{response.NotebookSlug}'.";

    private static string Capitalize(string value)
        => string.IsNullOrWhiteSpace(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
