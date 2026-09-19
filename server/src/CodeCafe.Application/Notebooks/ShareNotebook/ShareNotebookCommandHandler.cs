using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.ShareNotebook;
namespace CodeCafe.Application.Notebooks.ShareNotebook;

public sealed class ShareNotebookCommandHandler : ICommandHandler<ShareNotebookCommand, Result>
{
    public Task<Result> Handle(ShareNotebookCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
