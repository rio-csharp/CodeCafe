using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetMyNotebooks;

public sealed class GetMyNotebooksQueryHandler(
    INotebookReadService notebookReadService)
    : IQueryHandler<GetMyNotebooksQuery, IReadOnlyList<NotebookSummaryModel>>
{
    public async Task<IReadOnlyList<NotebookSummaryModel>> Handle(
        GetMyNotebooksQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookReadService.GetMyNotebooksAsync(
            request.CurrentUserId,
            request.Search,
            cancellationToken,
            request.Limit,
            request.Offset);
    }
}
