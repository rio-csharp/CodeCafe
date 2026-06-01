using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebookItems;

public sealed class GetPublicNotebookItemsQueryHandler(
    INotebookQueryService notebookQueryService)
    : IQueryHandler<GetPublicNotebookItemsQuery, NotesResult<IReadOnlyList<NotebookItemModel>>>
{
    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> Handle(
        GetPublicNotebookItemsQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookQueryService.GetPublicNotebookItemsAsync(
            request.Slug,
            cancellationToken);
    }
}
