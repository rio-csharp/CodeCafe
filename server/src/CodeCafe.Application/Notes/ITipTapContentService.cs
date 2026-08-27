using System.Text.Json;

namespace CodeCafe.Application.Notes;

public interface ITipTapContentService
{
    const int MaxDepth = 64;
    const int MaxNodeCount = 5000;
    const int MaxTextLength = 200_000;

    NotesResult<TipTapContentModel> NormalizePageContent(JsonElement? contentJson);
}

public sealed record TipTapContentModel(string? ContentJson, string? PlainTextContent);
