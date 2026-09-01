using CodeCafe.Domain.Common;
using CodeCafe.Domain.Notes.Enums;

namespace CodeCafe.Domain.Notes.Events;

public sealed record NotebookVisibilityChangedDomainEvent(
    Guid NotebookId,
    NotebookVisibility OldVisibility,
    NotebookVisibility NewVisibility,
    DateTimeOffset OccurredAtUtc
) : IDomainEvent;
