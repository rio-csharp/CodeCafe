using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
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
    public async Task<Result<PageDetailsDto>> Handle(CreatePageCommand command, CancellationToken cancellationToken)
    {
        var context = await NotebookAccess.RequireWriterAsync(command.NotebookIdOrSlug, currentUserAccessor, notebooks, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure<PageDetailsDto>(error);
        }

        var notebook = context.Value!.Notebook;

        var parent = command.ParentPath is not null
            ? await PageHierarchy.ResolveByPathAsync(notebook.Id, command.ParentPath, pages, cancellationToken)
            : null;
        if (command.ParentPath is not null && parent is null)
        {
            return Result.Failure<PageDetailsDto>(PageErrors.ParentNotFound);
        }

        // New pages append after the last sibling, matching how tree UIs grow.
        var siblings = await pages.ListChildrenAsync(notebook.Id, parent?.Id, cancellationToken);
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

        // The caller never picked the slug, so losing the race is worth re-keying rather than failing.
        var save = await SlugConflict.SaveAsync(
            unitOfWork,
            ct => ReKeyAsync(page, notebook.Id, title, ct),
            PageErrors.SlugAlreadyTaken,
            cancellationToken
        );
        if (save.Error is { } conflict)
        {
            return Result.Failure<PageDetailsDto>(conflict);
        }

        // Assemble the DTO once the save has stuck, not on every attempt.
        var ancestors = parent is null
            ? []
            : (await PageHierarchy.LoadAncestorsAsync(parent, pages, cancellationToken)).Append(parent).ToList();
        return Result.Success(
            PageDetailsMapping.ToDto(page, PageHierarchy.PathOf(page, ancestors), isFavorite: false, canWrite: true, shareUserNames: new Dictionary<Guid, string>())
        );
    }

    // Keeps the same page and moves it to a fresh candidate; false means none is left.
    private async Task<bool> ReKeyAsync(Page page, Guid notebookId, string title, CancellationToken cancellationToken)
    {
        var slug = await FindAvailableSlugAsync(notebookId, title, pages, cancellationToken);
        if (slug is null)
        {
            return false;
        }

        page.ChangeSlug(slug);
        return true;
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
