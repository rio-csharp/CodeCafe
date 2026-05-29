using System.Text.Json;

namespace CodeCafe.Application.Notes;

public interface ITipTapContentService
{
    NotesResult<TipTapContentModel> NormalizePageContent(JsonElement? contentJson, string? pageTitle = null);
}

public sealed record TipTapContentModel(
    string? ContentJson,
    string? PlainTextContent);
