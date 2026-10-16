using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.CreateNotebook;

public sealed class CreateNotebookCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<CreateNotebookCommand, Result<NotebookDetailsDto>>
{
    public async Task<Result<NotebookDetailsDto>> Handle(CreateNotebookCommand command, CancellationToken cancellationToken)
    {
        var resolved = await CurrentUserResolver.RequireAsync(currentUserAccessor, users, cancellationToken);
        if (resolved.Error is { } error)
        {
            return Result.Failure<NotebookDetailsDto>(error);
        }

        var user = resolved.Value!;

        var requestedSlug = command.Slug is not null ? NotebookSlug.Normalize(command.Slug) : null;
        var title = command.Title.Trim();
        var description = command.Description?.Trim() is { Length: > 0 } trimmedDescription ? trimmedDescription : null;

        var slug = await ResolveSlugAsync(requestedSlug, title, notebooks, cancellationToken);
        if (slug is null)
        {
            return Result.Failure<NotebookDetailsDto>(NotebookErrors.SlugAlreadyTaken);
        }

        var notebook = Notebook.Create(user.Id, title, description, slug, command.Visibility);
        await notebooks.AddAsync(notebook, cancellationToken);

        // An explicit slug is the caller's to fix; a generated one the user never picked, so only
        // that case is worth re-keying.
        var save = await SlugConflict.SaveAsync(
            unitOfWork,
            requestedSlug is null ? ct => ReKeyAsync(notebook, title, ct) : null,
            NotebookErrors.SlugAlreadyTaken,
            cancellationToken
        );
        if (save.Error is { } conflict)
        {
            return Result.Failure<NotebookDetailsDto>(conflict);
        }

        return Result.Success(ToDto(notebook));
    }

    // Keeps the same notebook and moves it to a fresh candidate; false means none is left.
    private async Task<bool> ReKeyAsync(Notebook notebook, string title, CancellationToken cancellationToken)
    {
        var slug = await ResolveSlugAsync(null, title, notebooks, cancellationToken);
        if (slug is null)
        {
            return false;
        }

        notebook.ChangeSlug(slug);
        return true;
    }

    // An explicit slug is final: a collision is the caller's to fix, not something to paper over
    // with a suffix. Generated slugs fall back to a suffixed variant instead.
    private static async Task<string?> ResolveSlugAsync(
        string? requestedSlug,
        string title,
        INotebookRepository notebooks,
        CancellationToken cancellationToken
    )
    {
        if (requestedSlug is not null)
        {
            return await notebooks.FindBySlugAsync(requestedSlug, cancellationToken) is null
                ? requestedSlug
                : null;
        }

        var candidates = await SlugAvailability.FindAvailableAsync(
            NotebookSlug.GenerateFromTitle(title),
            Notebook.MaxSlugLength,
            1,
            async (candidate, ct) => await notebooks.FindBySlugAsync(candidate, ct) is not null,
            cancellationToken
        );

        return candidates.Count > 0 ? candidates[0] : null;
    }

    private static NotebookDetailsDto ToDto(Notebook notebook) => new(
        notebook.Id,
        notebook.Title,
        notebook.Description,
        notebook.Slug,
        notebook.Visibility,
        HasAccessCode: false,
        Tags: [],
        Shares: [],
        PageCount: 0,
        notebook.CreatedAtUtc,
        notebook.UpdatedAtUtc,
        // The caller just created the notebook.
        IsOwner: true,
        CanWrite: true
    );
}
