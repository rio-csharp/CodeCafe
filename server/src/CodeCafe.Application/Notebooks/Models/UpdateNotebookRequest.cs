namespace CodeCafe.Application.Notebooks.Models;

public sealed record UpdateNotebookRequest(
    string? Title,
    string? Description,
    NotebookVisibility? Visibility);
