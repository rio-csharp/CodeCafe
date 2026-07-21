using CodeCafe.Modules.Notes.Application.Notes;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace CodeCafe.Modules.Mcp.Tools.Notes;

public static class NotesMcpResultMapper
{
    public static CallToolResult Success<T>(T value, string text) where T : class
    {
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = NotesMcpContentFormatter.Format(value, text) }],
            StructuredContent = JsonSerializer.SerializeToElement(value, NotesMcpSupport.SerializerOptions)
        };
    }

    public static CallToolResult Failure(NotesError error)
    {
        var response = new McpToolErrorResponse(
            error.Code,
            error.Message,
            error.Field,
            error.Kind is NotesFailureKind.Conflict,
            NotesMcpErrorAdvisor.GetSuggestion(error.Code),
            error.Details);

        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = NotesMcpContentFormatter.FormatError(response) }],
            StructuredContent = JsonSerializer.SerializeToElement(response, NotesMcpSupport.SerializerOptions)
        };
    }
}
