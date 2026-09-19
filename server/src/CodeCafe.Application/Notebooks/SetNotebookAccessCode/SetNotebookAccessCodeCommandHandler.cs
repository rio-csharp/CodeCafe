using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.SetNotebookAccessCode;
namespace CodeCafe.Application.Notebooks.SetNotebookAccessCode;

public sealed class SetNotebookAccessCodeCommandHandler : ICommandHandler<SetNotebookAccessCodeCommand, Result>
{
    public Task<Result> Handle(SetNotebookAccessCodeCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
