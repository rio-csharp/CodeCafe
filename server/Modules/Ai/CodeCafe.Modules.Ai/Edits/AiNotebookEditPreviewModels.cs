using System.Text.Json;

namespace CodeCafe.Ai.Edits;

public sealed record AiNotebookEditResponse(
    Guid ProposalId,
    string PreviewPath,
    string ApplyPath,
    string DiscardPath,
    DateTimeOffset ExpiresAtUtc,
    string Operation,
    string Mode,
    bool Applied,
    string Summary,
    Guid NotebookId,
    string NotebookSlug,
    string NotebookTitle,
    Guid? PageId,
    string Title,
    string? PagePath,
    string? ParentPath,
    JsonElement? BeforeContentJson,
    string? BeforePlainTextContent,
    JsonElement AfterContentJson,
    string? AfterPlainTextContent,
    JsonElement? OperationsJson,
    int AfterContentJsonBytes,
    int AfterPlainTextLength,
    int AfterTipTapNodeCount,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? SavedAtUtc);
