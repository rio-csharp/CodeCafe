using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.GetNotebookTree;
namespace CodeCafe.Application.Notebooks.GetNotebookTree;

public sealed class GetNotebookTreeQueryHandler : IQueryHandler<GetNotebookTreeQuery, Result<NotebookTreeDto>>
{
    public Task<Result<NotebookTreeDto>> Handle(GetNotebookTreeQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
