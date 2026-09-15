using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Trash.Commands;
namespace CodeCafe.Application.Trash.Handlers;

public sealed class PurgeNotebookCommandHandler : ICommandHandler<PurgeNotebookCommand, Result>
{
    public Task<Result> Handle(PurgeNotebookCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
