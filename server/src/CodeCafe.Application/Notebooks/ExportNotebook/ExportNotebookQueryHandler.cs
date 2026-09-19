using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.ExportNotebook;
namespace CodeCafe.Application.Notebooks.ExportNotebook;

public sealed class ExportNotebookQueryHandler : IQueryHandler<ExportNotebookQuery, Result<NotebookExportDto>>
{
    public Task<Result<NotebookExportDto>> Handle(ExportNotebookQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
