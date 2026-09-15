using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.Commands;

public sealed record SetNotebookAccessCodeCommand(string NotebookIdOrSlug, string? AccessCode) : ICommand<Result>;
