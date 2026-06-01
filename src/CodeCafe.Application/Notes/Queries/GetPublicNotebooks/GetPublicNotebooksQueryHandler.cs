using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebooks;

public sealed class GetPublicNotebooksQueryHandler(
    INotebookQueryService notebookQueryService)
    : IQueryHandler<GetPublicNotebooksQuery, IReadOnlyList<NotebookSummaryModel>>
{
    public async Task<IReadOnlyList<NotebookSummaryModel>> Handle(
        GetPublicNotebooksQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookQueryService.GetPublicNotebooksAsync(
            request.Search,
            request.CurrentUserId,
            cancellationToken);
    }
}
