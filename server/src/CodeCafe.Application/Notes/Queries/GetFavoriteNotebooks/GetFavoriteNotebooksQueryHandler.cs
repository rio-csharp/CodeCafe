using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetFavoriteNotebooks;

public sealed class GetFavoriteNotebooksQueryHandler(INotebookReadService notebookReadService)
    : IQueryHandler<GetFavoriteNotebooksQuery, IReadOnlyList<NotebookSummaryModel>>
{
    public Task<IReadOnlyList<NotebookSummaryModel>> Handle(GetFavoriteNotebooksQuery request, CancellationToken cancellationToken)
        => notebookReadService.GetFavoriteNotebooksAsync(
            request.CurrentUserId,
            request.Search,
            cancellationToken,
            request.Limit,
            request.Offset
        );
}
