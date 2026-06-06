using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookById;

public sealed class GetNotebookByIdQueryHandler(
    INotebookReadService notebookReadService)
    : IQueryHandler<GetNotebookByIdQuery, NotesResult<NotebookDetailModel>>
{
    public async Task<NotesResult<NotebookDetailModel>> Handle(
        GetNotebookByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookReadService.GetNotebookByIdAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken,
            request.IncludeArchived,
            request.IncludeItems);
    }
}
