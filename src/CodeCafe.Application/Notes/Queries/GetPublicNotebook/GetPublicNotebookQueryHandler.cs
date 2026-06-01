using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebook;

public sealed class GetPublicNotebookQueryHandler(
    INotebookQueryService notebookQueryService)
    : IQueryHandler<GetPublicNotebookQuery, NotesResult<NotebookDetailModel>>
{
    public async Task<NotesResult<NotebookDetailModel>> Handle(
        GetPublicNotebookQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookQueryService.GetPublicNotebookAsync(
            request.Slug,
            request.CurrentUserId,
            cancellationToken,
            request.IncludeArchived);
    }
}
