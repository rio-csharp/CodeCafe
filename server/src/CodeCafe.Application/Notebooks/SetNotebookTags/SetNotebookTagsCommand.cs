using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.SetNotebookTags;

public sealed record SetNotebookTagsCommand(string NotebookIdOrSlug, IReadOnlyList<string> Tags)
    : ICommand<Result>;
