using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.ChangeNotebookSlug;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
namespace CodeCafe.Application.Notebooks.ChangeNotebookSlug;

public sealed class ChangeNotebookSlugCommandHandler : ICommandHandler<ChangeNotebookSlugCommand, Result<NotebookDetailsDto>>
{
    public Task<Result<NotebookDetailsDto>> Handle(ChangeNotebookSlugCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
