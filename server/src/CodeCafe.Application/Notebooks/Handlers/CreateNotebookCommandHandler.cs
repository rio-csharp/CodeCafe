using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Commands;
using CodeCafe.Application.Notebooks.Models;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class CreateNotebookCommandHandler : ICommandHandler<CreateNotebookCommand, Result<NotebookDetailsDto>>
{
    public Task<Result<NotebookDetailsDto>> Handle(CreateNotebookCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
