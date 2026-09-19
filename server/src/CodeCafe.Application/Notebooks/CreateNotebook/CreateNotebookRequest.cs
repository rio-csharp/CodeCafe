using CodeCafe.Application.Notebooks.Shared;
namespace CodeCafe.Application.Notebooks.CreateNotebook;

public sealed record CreateNotebookRequest(
    string Title,
    string? Description,
    string? Slug,
    NotebookVisibility Visibility);
