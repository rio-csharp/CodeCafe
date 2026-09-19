using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

using CodeCafe.Application.Pages.RevokePageShare;
namespace CodeCafe.Application.Pages.SharePage;

public sealed record SharePageCommand(Guid PageId, string Email, CollaboratorRole Role)
    : ICommand<Result>;
public sealed record RevokePageShareCommand(Guid PageId, Guid UserId) : ICommand<Result>;
