using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebookItem;

public sealed record GetPublicNotebookItemQuery(
    string Slug,
    string Path) : IQuery<NotesResult<NotebookItemModel>>;
