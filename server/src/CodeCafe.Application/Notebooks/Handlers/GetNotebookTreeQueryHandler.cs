using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;
using CodeCafe.Application.Notebooks.Queries;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class GetNotebookTreeQueryHandler : IQueryHandler<GetNotebookTreeQuery, Result<NotebookTreeDto>>
{
    public Task<Result<NotebookTreeDto>> Handle(GetNotebookTreeQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
