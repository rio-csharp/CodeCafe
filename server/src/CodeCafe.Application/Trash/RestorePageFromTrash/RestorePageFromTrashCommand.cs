using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.RestorePageFromTrash;

public sealed record RestorePageFromTrashCommand(Guid PageId) : ICommand<Result>;
