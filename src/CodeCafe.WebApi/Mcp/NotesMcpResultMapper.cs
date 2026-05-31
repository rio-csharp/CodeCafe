using CodeCafe.Application.Notes;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace CodeCafe.WebApi.Mcp;

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
        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = error.Message }],
            StructuredContent = JsonSerializer.SerializeToElement(new McpToolErrorResponse(
                error.Code,
                error.Message,
                error.Kind is NotesFailureKind.Conflict,
                NotesMcpErrorAdvisor.GetSuggestion(error.Code)), NotesMcpSupport.SerializerOptions)
        };
    }
}
