using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Commands;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class SetNotebookFavoriteCommandHandler : ICommandHandler<SetNotebookFavoriteCommand, Result>
{
    public Task<Result> Handle(SetNotebookFavoriteCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
