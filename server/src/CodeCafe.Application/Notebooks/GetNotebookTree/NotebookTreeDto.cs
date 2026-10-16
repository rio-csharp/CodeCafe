namespace CodeCafe.Application.Notebooks.GetNotebookTree;

public sealed record NotebookTreeDto(Guid NotebookId, IReadOnlyList<PageTreeNodeDto> Roots);
