using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.SharePage;
namespace CodeCafe.Application.Pages.SharePage;

public sealed class SharePageCommandHandler : ICommandHandler<SharePageCommand, Result>
{
    public Task<Result> Handle(SharePageCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
