using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.PurgeTrashedNotebooks;

public sealed record PurgeTrashedNotebooksCommand() : ICommand<Result>;
