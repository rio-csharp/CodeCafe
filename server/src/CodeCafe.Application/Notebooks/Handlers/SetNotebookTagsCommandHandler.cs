using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Commands;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class SetNotebookTagsCommandHandler : ICommandHandler<SetNotebookTagsCommand, Result>
{
    public Task<Result> Handle(SetNotebookTagsCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
