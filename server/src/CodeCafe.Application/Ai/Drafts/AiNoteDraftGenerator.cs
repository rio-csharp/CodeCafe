using CodeCafe.Application.Notes;

namespace CodeCafe.Application.Ai.Drafts;

public interface IAiNoteDraftGenerator
{
    /// <summary>
    /// Generates a draft from the model.
    /// </summary>
    /// <exception cref="AiProviderException">
    /// Implementations must translate provider failures into this: <see cref="AiFailureKind.Upstream"/>
    /// when the provider errored or was unreachable, <see cref="AiFailureKind.Timeout"/> when it did not
    /// answer in time, and <see cref="AiFailureKind.Unprocessable"/> when the response cannot be used.
    /// Letting an SDK or HTTP exception escape turns a handled failure into a 500.
    /// </exception>
    Task<AiNoteDraftResult> GenerateDraftAsync(
        AiNoteDraftGenerationContext context,
        CancellationToken cancellationToken);
}

public sealed record AiNoteDraftGenerationContext(
    Guid CurrentUserId,
    string Intent,
    string Prompt,
    string Locale,
    NotebookContextModel Notebook,
    NotebookItemModel? ActivePage);

public sealed record AiNoteDraftResult(
    string Markdown);
