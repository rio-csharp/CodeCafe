using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookById;

public sealed class GetNotebookByIdQueryHandler(
    INotebookQueryService notebookQueryService)
    : IQueryHandler<GetNotebookByIdQuery, NotesResult<NotebookDetailModel>>
{
    public async Task<NotesResult<NotebookDetailModel>> Handle(
        GetNotebookByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookQueryService.GetNotebookByIdAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken,
            request.IncludeArchived);
    }
}
