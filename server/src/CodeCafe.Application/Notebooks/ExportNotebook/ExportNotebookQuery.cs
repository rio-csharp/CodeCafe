using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.ExportNotebook;

public sealed record ExportNotebookQuery(string NotebookIdOrSlug, string? AccessCode = null)
    : IQuery<Result<NotebookExportDto>>;
