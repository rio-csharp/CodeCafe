using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.DeleteNotebook;

public sealed record DeleteNotebookCommand(string NotebookIdOrSlug) : ICommand<Result>;
