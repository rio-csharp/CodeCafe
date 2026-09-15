namespace CodeCafe.Application.Notebooks.Models;

public sealed record CreateNotebookRequest(
    string Title,
    string? Description,
    string? Slug,
    NotebookVisibility Visibility);
