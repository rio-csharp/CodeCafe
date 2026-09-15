using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Commands;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class ShareNotebookCommandHandler : ICommandHandler<ShareNotebookCommand, Result>
{
    public Task<Result> Handle(ShareNotebookCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
