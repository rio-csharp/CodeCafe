using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookBySlug;

public sealed class GetNotebookBySlugQueryHandler(
    INotebookQueryService notebookQueryService)
    : IQueryHandler<GetNotebookBySlugQuery, NotesResult<NotebookDetailModel>>
{
    public async Task<NotesResult<NotebookDetailModel>> Handle(
        GetNotebookBySlugQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookQueryService.GetNotebookBySlugAsync(
            request.Slug,
            request.CurrentUserId,
            cancellationToken,
            request.IncludeArchived);
    }
}
