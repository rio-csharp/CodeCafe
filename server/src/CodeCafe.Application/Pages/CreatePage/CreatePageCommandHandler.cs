using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.CreatePage;

public sealed class CreatePageCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUnitOfWork unitOfWork
) : ICommandHandler<CreatePageCommand, Result<PageDetailsDto>>
{
    // Bounded retries: each attempt draws a fresh random suffix, so a couple of tries is plenty.
    private const int MaxSaveAttempts = 3;

    public async Task<Result<PageDetailsDto>> Handle(CreatePageCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var notebook = userId is not null
            ? await notebooks.FindByIdOrSlugAsync(command.NotebookIdOrSlug, cancellationToken)
            : null;
        if (notebook is null || !PageAccess.CanWrite(notebook, page: null, ancestors: [], userId!.Value))
        {
            return Result.Failure<PageDetailsDto>(NotebookErrors.NotFound);
        }

        var parent = command.ParentPath is not null
            ? await PageHierarchy.ResolveByPathAsync(notebook.Id, command.ParentPath, pages, cancellationToken)
            : null;
        if (command.ParentPath is not null && parent is null)
        {
            return Result.Failure<PageDetailsDto>(PageErrors.ParentNotFound);
        }

        // New pages append after the last sibling, matching how tree UIs grow.
        var siblings = await pages.ListSiblingsAsync(notebook.Id, parent?.Id, cancellationToken);
        var last = siblings.Count > 0 ? siblings[^1] : null;

        var title = command.Title.Trim();
        var slug = await FindAvailableSlugAsync(notebook.Id, title, pages, cancellationToken);
        if (slug is null)
        {
            return Result.Failure<PageDetailsDto>(PageErrors.SlugAlreadyTaken);
        }

        var page = Page.Create(notebook.Id, parent?.Id, title, slug, SiblingSortKeys.KeyForInsert(siblings, siblings.Count));
        PageChain.Link(page, notebook, parent, last);
        await pages.AddAsync(page, cancellationToken);

        return await SaveWithSlugRetryAsync(page, parent, notebook.Id, title, cancellationToken);
    }

    // Persist, recovering from slug races: each retry draws a fresh candidate on the same page.
    private async Task<Result<PageDetailsDto>> SaveWithSlugRetryAsync(
        Page page,
        Page? parent,
        Guid notebookId,
        string title,
        CancellationToken cancellationToken
    )
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (UniqueConstraintViolationException)
            {
                // The slug check raced with a concurrent create that took the slug; the caller
                // never picked it, so retry with a fresh candidate on the same page.
                if (attempt == MaxSaveAttempts)
                {
                    return Result.Failure<PageDetailsDto>(PageErrors.SlugAlreadyTaken);
                }

                var slug = await FindAvailableSlugAsync(notebookId, title, pages, cancellationToken);
                if (slug is null)
                {
                    return Result.Failure<PageDetailsDto>(PageErrors.SlugAlreadyTaken);
                }

                page.ChangeSlug(slug);
            }
        }

        // Assemble the DTO once the save has stuck, not on every attempt.
        var ancestors = parent is null
            ? []
            : (await PageHierarchy.LoadAncestorsAsync(parent, pages, cancellationToken)).Append(parent).ToList();
        return Result.Success(
            PageDetailsMapping.ToDto(page, PageHierarchy.PathOf(page, ancestors), isFavorite: false, shareUserNames: new Dictionary<Guid, string>())
        );
    }

    private static async Task<string?> FindAvailableSlugAsync(Guid notebookId, string title, IPageRepository pages, CancellationToken cancellationToken)
    {
        var candidates = await SlugAvailability.FindAvailableAsync(
            PageSlug.GenerateFromTitle(title),
            Page.MaxSlugLength,
            1,
            async (candidate, ct) => await pages.FindBySlugAsync(notebookId, candidate, ct) is not null,
            cancellationToken
        );

        return candidates.Count > 0 ? candidates[0] : null;
    }
}
