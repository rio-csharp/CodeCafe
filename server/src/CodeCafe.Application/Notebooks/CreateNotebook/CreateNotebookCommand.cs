using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.CreateNotebook;

public sealed record CreateNotebookCommand(
    string Title,
    string? Description,
    string? Slug,
    NotebookVisibility Visibility) : ICommand<Result<NotebookDetailsDto>>;
