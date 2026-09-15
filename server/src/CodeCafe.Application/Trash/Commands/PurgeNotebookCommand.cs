using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.Commands;

public sealed record PurgeNotebookCommand(Guid NotebookId) : ICommand<Result>;
