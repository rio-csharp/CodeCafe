using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.GetNotebookDetails;

namespace CodeCafe.Application.Notebooks.ImportNotebook;

public sealed class ImportNotebookCommandHandler : ICommandHandler<ImportNotebookCommand, Result<NotebookDetailsDto>>
{
    public Task<Result<NotebookDetailsDto>> Handle(ImportNotebookCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
