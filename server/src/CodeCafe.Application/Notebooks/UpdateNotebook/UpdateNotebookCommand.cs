using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Application.Notebooks.Shared;

namespace CodeCafe.Application.Notebooks.UpdateNotebook;

public sealed record UpdateNotebookCommand(
    string NotebookIdOrSlug,
    string? Title,
    string? Description,
    NotebookVisibility? Visibility) : ICommand<Result<NotebookDetailsDto>>;
