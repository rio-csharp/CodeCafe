namespace CodeCafe.Host.Mcp;

internal static class NotesMcpErrorAdvisor
{
    public static string? GetSuggestion(string code)
    {
        return code switch
        {
            "content_too_large" or "upload_too_large" or "upload_chunk_too_large" or "invalid_content_json" or "invalid_blocks" or "markdown_conversion_failed" or "invalid_tiptap_document"
                => $"Call {NotesMcpToolNames.GetLimits} to inspect MCP byte and TipTap node/text limits. For larger content, prefer {NotesMcpToolNames.PrepareHttpUpload} to upload Markdown directly via HTTP, then pass the returned upload id to the page tool. Only fall back to {NotesMcpToolNames.CreateUpload} and {NotesMcpToolNames.AppendUploadChunk} if HTTP upload is not available.",
            "notebook_item_not_archived"
                => $"Call {NotesMcpToolNames.ArchiveItem} first, then retry {NotesMcpToolNames.DeleteItem}.",
            "content_conflict"
                => $"Call {NotesMcpToolNames.GetPage} again to refresh the page state, then retry with the latest expectedUpdatedAtUtc value.",
            "upload_not_found"
                => $"The upload session expired or was discarded. For Markdown files, prefer {NotesMcpToolNames.PrepareHttpUpload} and upload via HTTP. Otherwise start a fresh upload with {NotesMcpToolNames.CreateUpload}, append chunks with {NotesMcpToolNames.AppendUploadChunk}, then retry the import.",
            "block_index_out_of_range"
                => $"Call {NotesMcpToolNames.GetPage} to inspect the current doc.content length, then use a valid zero-based index.",
            "text_not_found"
                => $"Call {NotesMcpToolNames.GetPage} to verify the exact text present in the document before retrying.",
            "invalid_parent"
                => $"Verify that parentPath points to an existing folder by calling {NotesMcpToolNames.ListItems} first.",
            _ => null
        };
    }
}
