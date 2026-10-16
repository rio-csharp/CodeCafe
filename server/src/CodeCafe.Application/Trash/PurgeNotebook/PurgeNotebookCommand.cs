using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.PurgeNotebook;

public sealed record PurgeNotebookCommand(Guid NotebookId) : ICommand<Result>;
