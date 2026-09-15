using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;
using CodeCafe.Application.Notebooks.Queries;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class ExportNotebookQueryHandler : IQueryHandler<ExportNotebookQuery, Result<NotebookExportDto>>
{
    public Task<Result<NotebookExportDto>> Handle(ExportNotebookQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
