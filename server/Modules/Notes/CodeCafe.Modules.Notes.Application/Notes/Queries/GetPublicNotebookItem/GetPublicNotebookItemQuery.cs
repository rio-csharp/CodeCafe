using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Queries.GetPublicNotebookItem;

public sealed record GetPublicNotebookItemQuery(
    string Slug,
    string Path) : IQuery<NotesResult<NotebookItemModel>>;
