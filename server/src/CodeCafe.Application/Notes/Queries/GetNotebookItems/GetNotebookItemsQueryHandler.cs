using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookItems;

public sealed class GetNotebookItemsQueryHandler(INotebookReadService notebookReadService)
    : IQueryHandler<GetNotebookItemsQuery, NotesResult<IReadOnlyList<NotebookItemModel>>>
{
    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> Handle(GetNotebookItemsQuery request, CancellationToken cancellationToken)
        => notebookReadService.GetNotebookItemsAsync(
            request.NotebookId,
            request.CurrentUserId,
            request.Search,
            cancellationToken,
            request.IncludeArchived,
            request.IncludeContent
        );
}
