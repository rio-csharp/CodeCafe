using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.SetNotebookAccessCode;

public sealed record SetNotebookAccessCodeCommand(string NotebookIdOrSlug, string? AccessCode) : ICommand<Result>;
