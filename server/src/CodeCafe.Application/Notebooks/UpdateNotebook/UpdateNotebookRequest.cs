using CodeCafe.Application.Notebooks.Shared;
namespace CodeCafe.Application.Notebooks.UpdateNotebook;

public sealed record UpdateNotebookRequest(
    string? Title,
    string? Description,
    NotebookVisibility? Visibility);
