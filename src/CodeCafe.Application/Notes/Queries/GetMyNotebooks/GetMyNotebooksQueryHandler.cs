using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetMyNotebooks;

public sealed class GetMyNotebooksQueryHandler(
    INotebookQueryService notebookQueryService)
    : IQueryHandler<GetMyNotebooksQuery, IReadOnlyList<NotebookSummaryModel>>
{
    public async Task<IReadOnlyList<NotebookSummaryModel>> Handle(
        GetMyNotebooksQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookQueryService.GetMyNotebooksAsync(
            request.CurrentUserId,
            request.Search,
            cancellationToken);
    }
}
