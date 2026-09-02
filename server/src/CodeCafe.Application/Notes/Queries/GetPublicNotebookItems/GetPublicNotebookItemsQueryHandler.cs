using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebookItems;

public sealed class GetPublicNotebookItemsQueryHandler(INotebookReadService notebookReadService)
    : IQueryHandler<GetPublicNotebookItemsQuery, NotesResult<IReadOnlyList<NotebookItemModel>>>
{
    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> Handle(GetPublicNotebookItemsQuery request, CancellationToken cancellationToken)
        => notebookReadService.GetPublicNotebookItemsAsync(request.Slug, cancellationToken);
}
