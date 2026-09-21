using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Notebooks.ExportNotebook;

public sealed class ExportNotebookQueryHandler : IQueryHandler<ExportNotebookQuery, Result<NotebookExportDto>>
{
    public Task<Result<NotebookExportDto>> Handle(ExportNotebookQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
