using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Commands;
using CodeCafe.Application.Notebooks.Models;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class ChangeNotebookSlugCommandHandler : ICommandHandler<ChangeNotebookSlugCommand, Result<NotebookDetailsDto>>
{
    public Task<Result<NotebookDetailsDto>> Handle(ChangeNotebookSlugCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
