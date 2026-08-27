using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebookItem;

public sealed class GetPublicNotebookItemQueryHandler(INotebookReadService notebookReadService)
    : IQueryHandler<GetPublicNotebookItemQuery, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        GetPublicNotebookItemQuery request,
        CancellationToken cancellationToken
    )
    {
        return await notebookReadService.GetPublicNotebookItemAsync(
            request.Slug,
            request.Path,
            cancellationToken
        );
    }
}
