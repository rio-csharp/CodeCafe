using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookItems;

public sealed class GetNotebookItemsQueryHandler(
    INotebookReadService notebookReadService)
    : IQueryHandler<GetNotebookItemsQuery, NotesResult<IReadOnlyList<NotebookItemModel>>>
{
    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> Handle(
        GetNotebookItemsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.IncludeArchived)
        {
            var notebook = await notebookReadService.GetNotebookByIdAsync(
                request.NotebookId,
                request.CurrentUserId,
                cancellationToken);

            if (!notebook.Succeeded)
            {
                return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                    notebook.Error!.Kind,
                    notebook.Error.Code,
                    notebook.Error.Message);
            }

            if (notebook.Value!.OwnerId != request.CurrentUserId)
            {
                return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                    NotesFailureKind.Forbidden,
                    "notebook_forbidden",
                    "Only the notebook owner can view archived items.");
            }
        }

        return await notebookReadService.GetNotebookItemsAsync(
            request.NotebookId,
            request.CurrentUserId,
            request.Search,
            cancellationToken,
            request.IncludeArchived,
            request.IncludeContent);
    }
}
