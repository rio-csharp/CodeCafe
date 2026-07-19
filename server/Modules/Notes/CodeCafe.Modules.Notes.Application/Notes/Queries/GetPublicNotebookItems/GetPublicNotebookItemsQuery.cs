using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Queries.GetPublicNotebookItems;

public sealed record GetPublicNotebookItemsQuery(
    string Slug) : IQuery<NotesResult<IReadOnlyList<NotebookItemModel>>>;
