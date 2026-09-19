using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.SetNotebookTags;
namespace CodeCafe.Application.Notebooks.SetNotebookTags;

public sealed class SetNotebookTagsCommandHandler : ICommandHandler<SetNotebookTagsCommand, Result>
{
    public Task<Result> Handle(SetNotebookTagsCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
