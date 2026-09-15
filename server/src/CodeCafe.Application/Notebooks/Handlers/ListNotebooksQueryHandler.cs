using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;
using CodeCafe.Application.Notebooks.Queries;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class ListNotebooksQueryHandler : IQueryHandler<ListNotebooksQuery, Result<CursorPage<NotebookSummaryDto>>>
{
    public Task<Result<CursorPage<NotebookSummaryDto>>> Handle(ListNotebooksQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
