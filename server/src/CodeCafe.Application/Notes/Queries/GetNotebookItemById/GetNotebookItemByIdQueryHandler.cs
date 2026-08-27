using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookItemById;

public sealed class GetNotebookItemByIdQueryHandler(INotebookReadService notebookReadService)
    : IQueryHandler<GetNotebookItemByIdQuery, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        GetNotebookItemByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        return await notebookReadService.GetNotebookItemByIdAsync(
            request.NotebookId,
            request.ItemId,
            request.CurrentUserId,
            cancellationToken,
            request.IncludeArchived
        );
    }
}
