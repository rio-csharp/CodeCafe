using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.Commands;

public sealed record RevokeNotebookShareCommand(string NotebookIdOrSlug, Guid UserId) : ICommand<Result>;
