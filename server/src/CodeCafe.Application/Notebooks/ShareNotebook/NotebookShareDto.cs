using CodeCafe.Application.Common;

namespace CodeCafe.Application.Notebooks.ShareNotebook;

public sealed record NotebookShareDto(Guid UserId, string UserName, CollaboratorRole Role);
