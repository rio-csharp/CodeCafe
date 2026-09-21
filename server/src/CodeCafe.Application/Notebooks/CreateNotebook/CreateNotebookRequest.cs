using CodeCafe.Domain.Notebooks;
namespace CodeCafe.Application.Notebooks.CreateNotebook;

public sealed record CreateNotebookRequest(
    string Title,
    string? Description,
    string? Slug,
    NotebookVisibility Visibility);
