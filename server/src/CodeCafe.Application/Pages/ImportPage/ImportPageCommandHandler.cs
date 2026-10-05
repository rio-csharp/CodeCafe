using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Markdown;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.ImportPage;

// Imports a markdown page file as a NEW page in the target notebook (appended after the last
// sibling, mirroring CreatePage), recreating its block tree as one revision batch.
public sealed class ImportPageCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IBlockRepository blocks,
    IBlockRevisionRepository revisions,
    IChangeSourceAccessor changeSource,
    IUnitOfWork unitOfWork
) : ICommandHandler<ImportPageCommand, Result<PageDetailsDto>>
{
    public async Task<Result<PageDetailsDto>> Handle(ImportPageCommand command, CancellationToken cancellationToken)
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

        var parsed = MarkdownPageParser.Parse(command.Export.FileName, command.Export.Markdown);
        var title = parsed.Title.Trim();
        if (title.Length == 0)
        {
            return Result.Failure<PageDetailsDto>(PageErrors.InvalidImport);
        }

        if (title.Length > Page.MaxTitleLength)
        {
            title = title[..Page.MaxTitleLength].TrimEnd();
        }

        // New pages append after the last sibling, matching how tree UIs grow.
        var siblings = await pages.ListChildrenAsync(notebook.Id, parent?.Id, cancellationToken);
        var last = siblings.Count > 0 ? siblings[^1] : null;

        var slug = await FindAvailableSlugAsync(notebook.Id, title, pages, cancellationToken);
        if (slug is null)
        {
            return Result.Failure<PageDetailsDto>(PageErrors.SlugAlreadyTaken);
        }

        var page = Page.Create(notebook.Id, parent?.Id, title, slug, SiblingSortKeys.KeyForInsert(siblings, siblings.Count));
        PageChain.Link(page, notebook, parent, last);
        await pages.AddAsync(page, cancellationToken);

        // The whole import is one batch in the page history. One SaveChanges persists the page
        // and its blocks atomically, so no explicit transaction is needed.
        MarkdownBlockImporter.CreateBlocks(
            page,
            parsed.Blocks,
            parent: null,
            Guid.CreateVersion7(),
            blocks,
            revisions,
            changeSource.Source
        );

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
