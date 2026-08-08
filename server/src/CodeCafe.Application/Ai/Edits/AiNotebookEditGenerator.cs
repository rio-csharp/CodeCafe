using CodeCafe.Application.Notes;
using System.Text.Json;

namespace CodeCafe.Application.Ai.Edits;

public interface IAiNotebookEditGenerator
{
    /// <summary>
    /// Generates a proposed notebook edit from the model.
    /// </summary>
    /// <exception cref="AiProviderException">
    /// Implementations must translate provider failures into this: <see cref="AiFailureKind.Upstream"/>
    /// when the provider errored or was unreachable, <see cref="AiFailureKind.Timeout"/> when it did not
    /// answer in time, and <see cref="AiFailureKind.Unprocessable"/> when the response cannot be used.
    /// Letting an SDK, HTTP or JSON exception escape turns a handled failure into a 500.
    /// </exception>
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
