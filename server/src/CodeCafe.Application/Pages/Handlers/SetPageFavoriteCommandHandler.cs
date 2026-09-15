using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Commands;
namespace CodeCafe.Application.Pages.Handlers;

public sealed class SetPageFavoriteCommandHandler : ICommandHandler<SetPageFavoriteCommand, Result>
{
    public Task<Result> Handle(SetPageFavoriteCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
