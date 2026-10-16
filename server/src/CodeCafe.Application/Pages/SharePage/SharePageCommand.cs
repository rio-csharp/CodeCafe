using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Pages.SharePage;

public sealed record SharePageCommand(Guid PageId, string Email, CollaboratorRole Role)
    : ICommand<Result>;
