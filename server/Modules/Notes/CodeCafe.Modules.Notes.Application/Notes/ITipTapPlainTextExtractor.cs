using System.Text.Json;

namespace CodeCafe.Modules.Notes.Application.Notes;

public interface ITipTapPlainTextExtractor
{
    string? Extract(JsonElement? contentJson);
}
