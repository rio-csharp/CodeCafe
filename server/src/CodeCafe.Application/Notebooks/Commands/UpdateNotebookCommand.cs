using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;

namespace CodeCafe.Application.Notebooks.Commands;

public sealed record UpdateNotebookCommand(
    string NotebookIdOrSlug,
    string? Title,
    string? Description,
    NotebookVisibility? Visibility) : ICommand<Result<NotebookDetailsDto>>;
