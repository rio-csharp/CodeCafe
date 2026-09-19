using CodeCafe.Application.Common;

namespace CodeCafe.Application.Pages.Shared;

public sealed record PageShareDto(Guid UserId, string UserName, CollaboratorRole Role);
