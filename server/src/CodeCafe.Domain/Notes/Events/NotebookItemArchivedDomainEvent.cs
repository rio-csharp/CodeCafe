using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Notes.Events;

public sealed record NotebookItemArchivedDomainEvent(
    Guid NotebookId,
    Guid ItemId,
    Guid ArchivedByUserId,
    DateTimeOffset OccurredAtUtc
) : IDomainEvent;
