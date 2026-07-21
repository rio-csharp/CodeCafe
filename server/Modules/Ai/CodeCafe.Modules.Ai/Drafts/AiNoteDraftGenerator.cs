using CodeCafe.Modules.Notes.Application.Notes;

namespace CodeCafe.Modules.Ai.Drafts;

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
    NotebookContextModel Notebook,
    NotebookItemModel? ActivePage);

public sealed record AiNoteDraftResult(
    string Markdown);
