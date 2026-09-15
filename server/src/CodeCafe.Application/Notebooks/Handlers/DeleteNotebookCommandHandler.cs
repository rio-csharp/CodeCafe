using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Commands;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class DeleteNotebookCommandHandler : ICommandHandler<DeleteNotebookCommand, Result>
{
    public Task<Result> Handle(DeleteNotebookCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
