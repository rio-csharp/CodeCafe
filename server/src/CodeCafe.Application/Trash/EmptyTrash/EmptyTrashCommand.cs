using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.EmptyTrash;

public sealed record EmptyTrashCommand() : ICommand<Result>;
