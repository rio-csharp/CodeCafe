namespace CodeCafe.Application.Notebooks.GetNotebookSlugAvailability;

// `IsAvailable` is advisory: the slug can be taken between this check and the create or rename
// that follows it. The authoritative answer stays the conflict returned by those calls, and
// `Suggestions` names free alternatives the caller can offer instead.
public sealed record NotebookSlugAvailabilityDto(
    string Slug,
    bool IsAvailable,
    IReadOnlyList<string> Suggestions);
