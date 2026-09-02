using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookBySlug;

public sealed class GetNotebookBySlugQueryHandler(INotebookReadService notebookReadService)
    : IQueryHandler<GetNotebookBySlugQuery, NotesResult<NotebookDetailModel>>
{
    public Task<NotesResult<NotebookDetailModel>> Handle(GetNotebookBySlugQuery request, CancellationToken cancellationToken)
        => notebookReadService.GetNotebookBySlugAsync(
            request.Slug,
            request.CurrentUserId,
            cancellationToken,
            request.IncludeArchived,
            request.IncludeItems,
            request.IncludeContent,
            request.AccessCode
        );
}
