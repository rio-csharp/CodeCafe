using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookFavoriteStatus;

public sealed class GetNotebookFavoriteStatusQueryHandler(INotebookReadService notebookReadService)
    : IQueryHandler<GetNotebookFavoriteStatusQuery, NotesResult<NotebookFavoriteModel>>
{
    public Task<NotesResult<NotebookFavoriteModel>> Handle(GetNotebookFavoriteStatusQuery request, CancellationToken cancellationToken)
        => notebookReadService.GetNotebookFavoriteStatusAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken
        );
}
