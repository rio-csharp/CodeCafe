using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.SetNotebookFavorite;
namespace CodeCafe.Application.Notebooks.SetNotebookFavorite;

public sealed class SetNotebookFavoriteCommandHandler : ICommandHandler<SetNotebookFavoriteCommand, Result>
{
    public Task<Result> Handle(SetNotebookFavoriteCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
