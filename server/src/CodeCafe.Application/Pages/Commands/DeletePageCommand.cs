using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Pages.Commands;

public sealed record DeletePageCommand(Guid PageId) : ICommand<Result>;
