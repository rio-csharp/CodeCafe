using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.Commands;

public sealed record DeleteNotebookCommand(string NotebookIdOrSlug) : ICommand<Result>;
