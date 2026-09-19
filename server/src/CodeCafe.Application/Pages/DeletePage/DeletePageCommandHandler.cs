using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.DeletePage;
namespace CodeCafe.Application.Pages.DeletePage;

public sealed class DeletePageCommandHandler : ICommandHandler<DeletePageCommand, Result>
{
    public Task<Result> Handle(DeletePageCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
