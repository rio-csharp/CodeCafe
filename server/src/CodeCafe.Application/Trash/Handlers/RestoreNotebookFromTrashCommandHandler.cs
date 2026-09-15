using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Trash.Commands;
namespace CodeCafe.Application.Trash.Handlers;

public sealed class RestoreNotebookFromTrashCommandHandler : ICommandHandler<RestoreNotebookFromTrashCommand, Result>
{
    public Task<Result> Handle(RestoreNotebookFromTrashCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
