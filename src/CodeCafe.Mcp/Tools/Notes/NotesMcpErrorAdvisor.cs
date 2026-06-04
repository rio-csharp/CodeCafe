namespace CodeCafe.Mcp.Tools.Notes;

internal static class NotesMcpErrorAdvisor
{
    public static string? GetSuggestion(string code)
    {
        return code switch
        {
            "content_too_large" or "upload_too_large" or "upload_chunk_too_large" or "invalid_content_json" or "invalid_blocks" or "markdown_conversion_failed" or "invalid_tiptap_document"
                => $"Call {NotesMcpToolNames.GetLimits} to inspect MCP byte and TipTap node/text limits. For larger content, use {NotesMcpToolNames.CreateUpload} and {NotesMcpToolNames.AppendUploadChunk}, then pass the upload id to the page tool. MCP TipTap JSON rejects H1 headings; use H2 for body sections.",
            "notebook_item_not_archived"
                => $"Call {NotesMcpToolNames.ArchiveItem} first, then retry {NotesMcpToolNames.DeleteItem}.",
            "content_conflict"
                => $"Call {NotesMcpToolNames.GetPage} again to refresh the page state, then retry with the latest expectedUpdatedAtUtc value.",
            "upload_not_found"
                => $"Start a fresh upload with {NotesMcpToolNames.CreateUpload}, append chunks with {NotesMcpToolNames.AppendUploadChunk}, then retry the import.",
            "invalid_parent"
                => $"Verify that parentPath points to an existing folder by calling {NotesMcpToolNames.ListItems} first.",
            _ => null
        };
    }
}
