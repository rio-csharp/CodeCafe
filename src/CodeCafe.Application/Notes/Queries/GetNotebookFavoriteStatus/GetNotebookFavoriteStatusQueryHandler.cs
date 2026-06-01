using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookFavoriteStatus;

public sealed class GetNotebookFavoriteStatusQueryHandler(
    INotebookFavoriteService notebookFavoriteService)
    : IQueryHandler<GetNotebookFavoriteStatusQuery, NotesResult<NotebookFavoriteModel>>
{
    public async Task<NotesResult<NotebookFavoriteModel>> Handle(
        GetNotebookFavoriteStatusQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookFavoriteService.GetFavoriteStatusAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken);
    }
}
