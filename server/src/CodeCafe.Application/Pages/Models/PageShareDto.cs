using CodeCafe.Application.Common;

namespace CodeCafe.Application.Pages.Models;

public sealed record PageShareDto(Guid UserId, string UserName, CollaboratorRole Role);
