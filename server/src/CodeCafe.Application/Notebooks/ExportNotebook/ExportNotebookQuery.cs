using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.ExportNotebook;

namespace CodeCafe.Application.Notebooks.ExportNotebook;

public sealed record ExportNotebookQuery(string NotebookIdOrSlug)
    : IQuery<Result<NotebookExportDto>>;
