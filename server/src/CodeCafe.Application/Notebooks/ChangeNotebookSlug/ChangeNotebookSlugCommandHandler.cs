using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Application.Notebooks.Shared;

namespace CodeCafe.Application.Notebooks.ChangeNotebookSlug;

public sealed class ChangeNotebookSlugCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<ChangeNotebookSlugCommand, Result<NotebookDetailsDto>>
{
    public async Task<Result<NotebookDetailsDto>> Handle(ChangeNotebookSlugCommand command, CancellationToken cancellationToken)
    {
        var context = await NotebookAccess.RequireOwnerAsync(command.NotebookIdOrSlug, currentUserAccessor, notebooks, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure<NotebookDetailsDto>(error);
        }

        var notebook = context.Value!.Notebook;

        var newSlug = NotebookSlug.Normalize(command.NewSlug);
        if (string.Equals(notebook.Slug, newSlug, StringComparison.Ordinal))
        {
            return Result.Success(await NotebookDetailsMapping.ToDtoAsync(notebook, users, cancellationToken));
        }

        if (await notebooks.FindBySlugAsync(newSlug, cancellationToken) is not null)
        {
            return Result.Failure<NotebookDetailsDto>(NotebookErrors.SlugAlreadyTaken);
        }

        notebook.ChangeSlug(newSlug);

        // The slug is the only unique value on this aggregate, so the violation can only be a
        // concurrent rename that took the slug after the check above. The caller picked it, so
        // there is nothing to re-key.
        var save = await SlugConflict.SaveAsync(
            unitOfWork,
            reKeyAsync: null,
            NotebookErrors.SlugAlreadyTaken,
            cancellationToken
        );
        if (save.Error is { } conflict)
        {
            return Result.Failure<NotebookDetailsDto>(conflict);
        }

        return Result.Success(await NotebookDetailsMapping.ToDtoAsync(notebook, users, cancellationToken));
    }
}
