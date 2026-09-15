using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.Commands;

public sealed record SetNotebookTagsCommand(string NotebookIdOrSlug, IReadOnlyList<string> Tags)
    : ICommand<Result>;
