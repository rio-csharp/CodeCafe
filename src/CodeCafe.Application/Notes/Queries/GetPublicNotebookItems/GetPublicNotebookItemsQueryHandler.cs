using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebookItems;

public sealed class GetPublicNotebookItemsQueryHandler(
    INotebookReadService notebookReadService)
    : IQueryHandler<GetPublicNotebookItemsQuery, NotesResult<IReadOnlyList<NotebookItemModel>>>
{
    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> Handle(
        GetPublicNotebookItemsQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookReadService.GetPublicNotebookItemsAsync(
            request.Slug,
            cancellationToken);
    }
}
