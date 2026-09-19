using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Application.Notebooks.ExportNotebook;

namespace CodeCafe.Application.Notebooks.ImportNotebook;

public sealed record ImportNotebookCommand(NotebookExportDto Export) : ICommand<Result<NotebookDetailsDto>>;
