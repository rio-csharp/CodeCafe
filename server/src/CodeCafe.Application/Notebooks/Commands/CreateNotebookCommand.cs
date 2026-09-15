using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;

namespace CodeCafe.Application.Notebooks.Commands;

public sealed record CreateNotebookCommand(
    string Title,
    string? Description,
    string? Slug,
    NotebookVisibility Visibility) : ICommand<Result<NotebookDetailsDto>>;
