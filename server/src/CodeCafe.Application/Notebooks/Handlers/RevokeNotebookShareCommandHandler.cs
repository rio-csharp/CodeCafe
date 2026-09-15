using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Commands;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class RevokeNotebookShareCommandHandler : ICommandHandler<RevokeNotebookShareCommand, Result>
{
    public Task<Result> Handle(RevokeNotebookShareCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
