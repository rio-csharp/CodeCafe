using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookById;

public sealed class GetNotebookByIdQueryHandler(INotebookReadService notebookReadService)
    : IQueryHandler<GetNotebookByIdQuery, NotesResult<NotebookDetailModel>>
{
    public Task<NotesResult<NotebookDetailModel>> Handle(GetNotebookByIdQuery request, CancellationToken cancellationToken)
        => notebookReadService.GetNotebookByIdAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken,
            request.IncludeArchived,
            request.IncludeItems,
            request.IncludeContent
        );
}
