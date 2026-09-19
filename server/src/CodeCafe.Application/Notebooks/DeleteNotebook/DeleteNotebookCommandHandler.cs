using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.DeleteNotebook;
namespace CodeCafe.Application.Notebooks.DeleteNotebook;

public sealed class DeleteNotebookCommandHandler : ICommandHandler<DeleteNotebookCommand, Result>
{
    public Task<Result> Handle(DeleteNotebookCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
