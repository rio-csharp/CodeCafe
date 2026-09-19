using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Pages.RevokePageShare;

public sealed record RevokePageShareCommand(string PageId, Guid UserId) : ICommand<Result>;
