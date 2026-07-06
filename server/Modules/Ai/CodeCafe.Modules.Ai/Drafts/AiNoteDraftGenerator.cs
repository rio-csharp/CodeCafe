using CodeCafe.Application.Notes;

namespace CodeCafe.Ai.Drafts;

public interface IAiNoteDraftGenerator
{
    Task<AiNoteDraftResult> GenerateDraftAsync(
        AiNoteDraftGenerationContext context,
        CancellationToken cancellationToken);
}

public sealed record AiNoteDraftGenerationContext(
    Guid CurrentUserId,
    string Intent,
    string Prompt,
    string Locale,
    NotebookDetailModel Notebook,
    NotebookItemModel? ActivePage);

public sealed record AiNoteDraftResult(
    string Markdown);
