using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Commands;
namespace CodeCafe.Application.Pages.Handlers;

public sealed class SharePageCommandHandler : ICommandHandler<SharePageCommand, Result>
{
    public Task<Result> Handle(SharePageCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
