using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;
using CodeCafe.Application.Notebooks.Queries;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class GetNotebookDetailsQueryHandler : IQueryHandler<GetNotebookDetailsQuery, Result<NotebookDetailsDto>>
{
    public Task<Result<NotebookDetailsDto>> Handle(GetNotebookDetailsQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
