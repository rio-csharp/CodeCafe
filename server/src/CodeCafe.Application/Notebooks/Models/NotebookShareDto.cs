using CodeCafe.Application.Common;

namespace CodeCafe.Application.Notebooks.Models;

public sealed record NotebookShareDto(Guid UserId, string UserName, CollaboratorRole Role);
