namespace CodeCafe.Application.Notebooks.Models;

public sealed record NotebookTreeDto(Guid NotebookId, IReadOnlyList<PageTreeNodeDto> Roots);
