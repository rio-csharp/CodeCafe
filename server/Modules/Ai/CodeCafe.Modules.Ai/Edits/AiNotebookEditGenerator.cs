using CodeCafe.Application.Notes;
using System.Text.Json;

namespace CodeCafe.Modules.Ai.Edits;

public interface IAiNotebookEditGenerator
{
    Task<AiNotebookEditResult> GenerateEditAsync(
        AiNotebookEditGenerationContext context,
        CancellationToken cancellationToken);
}

public sealed record AiNotebookEditGenerationContext(
    Guid CurrentUserId,
    string RequestedOperation,
    string Prompt,
    string Locale,
    NotebookContextModel Notebook,
    NotebookItemModel? ActivePage);

public sealed record AiNotebookEditResult(
    string Operation,
    string Mode,
    string? Title,
    string? Summary,
    JsonElement? ContentJson,
    JsonElement? OperationsJson);
