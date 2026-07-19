using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Queries.GetPublicNotebookItem;

public sealed class GetPublicNotebookItemQueryHandler(
    INotebookReadService notebookReadService)
    : IQueryHandler<GetPublicNotebookItemQuery, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        GetPublicNotebookItemQuery request,
        CancellationToken cancellationToken)
    {
        return await notebookReadService.GetPublicNotebookItemAsync(
            request.Slug,
            request.Path,
            cancellationToken);
    }
}
