using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Commands;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class SetNotebookAccessCodeCommandHandler : ICommandHandler<SetNotebookAccessCodeCommand, Result>
{
    public Task<Result> Handle(SetNotebookAccessCodeCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
