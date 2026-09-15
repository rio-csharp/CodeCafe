using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Commands;
using CodeCafe.Application.Notebooks.Models;

namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class ImportNotebookCommandHandler : ICommandHandler<ImportNotebookCommand, Result<NotebookDetailsDto>>
{
    public Task<Result<NotebookDetailsDto>> Handle(ImportNotebookCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
