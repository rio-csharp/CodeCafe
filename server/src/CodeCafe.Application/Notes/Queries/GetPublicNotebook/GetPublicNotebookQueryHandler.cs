using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebook;

public sealed class GetPublicNotebookQueryHandler(INotebookReadService notebookReadService)
    : IQueryHandler<GetPublicNotebookQuery, NotesResult<NotebookDetailModel>>
{
    public Task<NotesResult<NotebookDetailModel>> Handle(GetPublicNotebookQuery request, CancellationToken cancellationToken)
        => notebookReadService.GetPublicNotebookAsync(
            request.Slug,
            request.CurrentUserId,
            cancellationToken,
            request.IncludeArchived,
            request.IncludeItems,
            request.IncludeContent
        );
}
