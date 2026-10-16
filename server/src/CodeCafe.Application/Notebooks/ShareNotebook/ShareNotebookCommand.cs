using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Notebooks.ShareNotebook;

public sealed record ShareNotebookCommand(string NotebookIdOrSlug, string Email, CollaboratorRole Role)
    : ICommand<Result>;
