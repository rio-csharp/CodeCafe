using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Trash.PurgeNotebook;

public sealed class PurgeNotebookCommandHandler : ICommandHandler<PurgeNotebookCommand, Result>
{
    public Task<Result> Handle(PurgeNotebookCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
