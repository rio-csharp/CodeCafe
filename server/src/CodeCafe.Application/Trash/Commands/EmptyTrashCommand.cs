using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.Commands;

public sealed record EmptyTrashCommand() : ICommand<Result>;
