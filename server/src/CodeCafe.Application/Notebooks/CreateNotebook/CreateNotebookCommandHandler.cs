using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
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
    // Bounded retries: each attempt draws a fresh random suffix, so a couple of tries is plenty.
    private const int MaxSaveAttempts = 3;

    public async Task<Result<NotebookDetailsDto>> Handle(CreateNotebookCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var user = userId is not null
            ? await users.FindByIdAsync(userId.Value, cancellationToken)
            : null;
        if (user is null)
        {
            return Result.Failure<NotebookDetailsDto>(AuthErrors.UserNotFound);
        }

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

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success(ToDto(notebook));
            }
            catch (UniqueConstraintViolationException)
            {
                // The availability check raced with a concurrent create that took the slug. An
                // explicit slug is the caller's to fix; a generated one the user never picked,
                // so keep the same notebook and retry with a fresh candidate.
                if (requestedSlug is not null || attempt == MaxSaveAttempts)
                {
                    return Result.Failure<NotebookDetailsDto>(NotebookErrors.SlugAlreadyTaken);
                }

                slug = await ResolveSlugAsync(null, title, notebooks, cancellationToken);
                if (slug is null)
                {
                    return Result.Failure<NotebookDetailsDto>(NotebookErrors.SlugAlreadyTaken);
                }

                notebook.ChangeSlug(slug);
            }
        }
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

        var candidates = await NotebookSlugAvailability.FindAvailableAsync(
            NotebookSlug.GenerateFromTitle(title),
            1,
            notebooks,
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
        notebook.UpdatedAtUtc
    );
}
