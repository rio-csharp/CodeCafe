using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Pages.SetPageFavorite;

public sealed class SetPageFavoriteCommandHandler : ICommandHandler<SetPageFavoriteCommand, Result>
{
    public Task<Result> Handle(SetPageFavoriteCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
