using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebookItems;

public sealed record GetPublicNotebookItemsQuery(
    string Slug
) : IQuery<NotesResult<IReadOnlyList<NotebookItemModel>>>;
