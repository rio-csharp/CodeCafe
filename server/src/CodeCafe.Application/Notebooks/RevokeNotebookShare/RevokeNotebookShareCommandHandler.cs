using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.RevokeNotebookShare;
namespace CodeCafe.Application.Notebooks.RevokeNotebookShare;

public sealed class RevokeNotebookShareCommandHandler : ICommandHandler<RevokeNotebookShareCommand, Result>
{
    public Task<Result> Handle(RevokeNotebookShareCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
