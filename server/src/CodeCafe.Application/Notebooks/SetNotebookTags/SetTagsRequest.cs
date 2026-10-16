namespace CodeCafe.Application.Notebooks.SetNotebookTags;

public sealed record SetTagsRequest(IReadOnlyList<string> Tags);
