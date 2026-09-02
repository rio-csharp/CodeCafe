using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetMyNotebooks;

public sealed class GetMyNotebooksQueryHandler(INotebookReadService notebookReadService)
    : IQueryHandler<GetMyNotebooksQuery, IReadOnlyList<NotebookSummaryModel>>
{
    public Task<IReadOnlyList<NotebookSummaryModel>> Handle(GetMyNotebooksQuery request, CancellationToken cancellationToken)
        => notebookReadService.GetMyNotebooksAsync(
            request.CurrentUserId,
            request.Search,
            cancellationToken,
            request.Limit,
            request.Offset
        );
}
