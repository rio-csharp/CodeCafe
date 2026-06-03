using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebooks;

public sealed class GetPublicNotebooksQueryHandler(
    INotebookReadService notebookReadService)
    : IQueryHandler<GetPublicNotebooksQuery, IReadOnlyList<NotebookSummaryModel>>
{
    public async Task<IReadOnlyList<NotebookSummaryModel>> Handle(
        GetPublicNotebooksQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookReadService.GetPublicNotebooksAsync(
            request.Search,
            request.CurrentUserId,
            cancellationToken);
    }
}
