using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Notes.Events;

public sealed record NotebookItemRestoredDomainEvent(
    Guid NotebookId,
    Guid ItemId,
    DateTimeOffset OccurredAtUtc
) : IDomainEvent;
