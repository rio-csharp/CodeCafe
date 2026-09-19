using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
namespace CodeCafe.Application.Notebooks.GetNotebookDetails;

public sealed class GetNotebookDetailsQueryHandler : IQueryHandler<GetNotebookDetailsQuery, Result<NotebookDetailsDto>>
{
    public Task<Result<NotebookDetailsDto>> Handle(GetNotebookDetailsQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
