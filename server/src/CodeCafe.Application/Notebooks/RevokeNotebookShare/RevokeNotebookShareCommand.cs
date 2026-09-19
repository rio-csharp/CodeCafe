using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.RevokeNotebookShare;

public sealed record RevokeNotebookShareCommand(string NotebookIdOrSlug, Guid UserId) : ICommand<Result>;
