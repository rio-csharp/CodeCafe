using System.Text.Json;

namespace CodeCafe.Application.Notes;

public interface ITipTapPlainTextExtractor
{
    string? Extract(JsonElement? contentJson);
}
