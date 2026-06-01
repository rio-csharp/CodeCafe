using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebookItem;

public sealed class GetPublicNotebookItemQueryHandler(
    INotebookQueryService notebookQueryService)
    : IQueryHandler<GetPublicNotebookItemQuery, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        GetPublicNotebookItemQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookQueryService.GetPublicNotebookItemAsync(
            request.Slug,
            request.Path,
            cancellationToken);
    }
}
