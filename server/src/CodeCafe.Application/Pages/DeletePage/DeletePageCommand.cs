using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Pages.DeletePage;

public sealed record DeletePageCommand(Guid PageId) : ICommand<Result>;
