using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Trash.RestoreNotebookFromTrash;

public sealed class RestoreNotebookFromTrashCommandHandler : ICommandHandler<RestoreNotebookFromTrashCommand, Result>
{
    public Task<Result> Handle(RestoreNotebookFromTrashCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
