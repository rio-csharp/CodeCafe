namespace CodeCafe.Application.Notebooks.Models;

public sealed record SetTagsRequest(IReadOnlyList<string> Tags);
