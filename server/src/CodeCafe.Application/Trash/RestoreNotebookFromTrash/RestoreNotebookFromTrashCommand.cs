using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.RestoreNotebookFromTrash;

public sealed record RestoreNotebookFromTrashCommand(Guid NotebookId) : ICommand<Result>;
