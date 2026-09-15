using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Pages.Commands;

public sealed record SharePageCommand(Guid PageId, string Email, CollaboratorRole Role)
    : ICommand<Result>;
public sealed record RevokePageShareCommand(Guid PageId, Guid UserId) : ICommand<Result>;
