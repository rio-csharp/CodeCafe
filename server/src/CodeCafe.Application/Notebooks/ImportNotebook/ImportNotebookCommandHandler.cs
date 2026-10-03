using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Exceptions;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Markdown;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Application.Revisions.Shared;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Notebooks.ImportNotebook;

public sealed class ImportNotebookCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users,
    INotebookRepository notebooks,
    IPageRepository pages,
    IBlockRepository blocks,
    IBlockRevisionRepository revisions,
    IChangeSourceAccessor changeSource,
    IUnitOfWork unitOfWork
) : ICommandHandler<ImportNotebookCommand, Result<NotebookDetailsDto>>
{
    public async Task<Result<NotebookDetailsDto>> Handle(
        ImportNotebookCommand command,
        CancellationToken cancellationToken
    )
    {
        var resolved = await CurrentUserResolver.RequireAsync(currentUserAccessor, users, cancellationToken);
        if (resolved.Error is { } error)
        {
            return Result.Failure<NotebookDetailsDto>(error);
        }

        var parsed = MarkdownNotebookParser.Parse(command.Export.FileName, command.Export.Markdown);
        var title = parsed.Title.Trim();
        if (title.Length == 0)
        {
            return Result.Failure<NotebookDetailsDto>(NotebookErrors.InvalidImport);
        }

        if (title.Length > Notebook.MaxTitleLength)
        {
            title = title[..Notebook.MaxTitleLength].TrimEnd();
        }

        var slug = await ResolveSlugAsync(title, notebooks, cancellationToken);
        if (slug is null)
        {
            return Result.Failure<NotebookDetailsDto>(NotebookErrors.SlugAlreadyTaken);
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var notebook = Notebook.Create(resolved.Value!.Id, title, null, slug, NotebookVisibility.Private);
        await notebooks.AddAsync(notebook, cancellationToken);

        // The whole import is one batch in the page histories.
        var batchId = Guid.CreateVersion7();
        var importedPages = new List<Page>();
        var usedPageSlugs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parsedPage in parsed.Pages)
        {
            var pageTitle = string.IsNullOrWhiteSpace(parsedPage.Title) ? "Untitled" : parsedPage.Title.Trim();
            if (pageTitle.Length > Page.MaxTitleLength)
            {
                pageTitle = pageTitle[..Page.MaxTitleLength].TrimEnd();
            }

            var pageSlug = await ResolvePageSlugAsync(notebook.Id, pageTitle, pages, usedPageSlugs, cancellationToken);
            if (pageSlug is null)
            {
                return Result.Failure<NotebookDetailsDto>(NotebookErrors.InvalidImport);
            }

            var last = importedPages.Count == 0 ? null : importedPages[^1];
            var page = Page.Create(
                notebook.Id,
                null,
                pageTitle,
                pageSlug,
                SiblingSortKeys.KeyForInsert(importedPages, importedPages.Count)
            );
            PageChain.Link(page, notebook, null, last);
            await pages.AddAsync(page, cancellationToken);
            importedPages.Add(page);
            usedPageSlugs.Add(pageSlug);

            MarkdownBlockImporter.CreateBlocks(page, parsedPage.Blocks, parent: null, batchId, blocks, revisions, changeSource.Source);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<NotebookDetailsDto>(NotebookErrors.SlugAlreadyTaken);
        }

        var pageCount = await pages.CountByNotebookAsync(notebook.Id, cancellationToken);
        return Result.Success(
            new NotebookDetailsDto(
                notebook.Id,
                notebook.Title,
                notebook.Description,
                notebook.Slug,
                notebook.Visibility,
                HasAccessCode: false,
                Tags: [],
                Shares: [],
                pageCount,
                notebook.CreatedAtUtc,
                notebook.UpdatedAtUtc
            )
        );
    }

    private static async Task<string?> ResolveSlugAsync(
        string title,
        INotebookRepository notebooks,
        CancellationToken cancellationToken
    )
    {
        var baseSlug = NotebookSlug.GenerateFromTitle(title);
        var candidates = await SlugAvailability.FindAvailableAsync(
            baseSlug,
            Notebook.MaxSlugLength,
            1,
            async (candidate, ct) => await notebooks.FindBySlugAsync(candidate, ct) is not null,
            cancellationToken
        );
        return candidates.Count == 0 ? null : candidates[0];
    }

    private static async Task<string?> ResolvePageSlugAsync(
        Guid notebookId,
        string title,
        IPageRepository pages,
        ISet<string> usedPageSlugs,
        CancellationToken cancellationToken
    )
    {
        var baseSlug = PageSlug.GenerateFromTitle(title);
        var candidates = await SlugAvailability.FindAvailableAsync(
            baseSlug,
            Page.MaxSlugLength,
            1,
            async (candidate, ct) => usedPageSlugs.Contains(candidate)
                || await pages.FindBySlugAsync(notebookId, candidate, ct) is not null,
            cancellationToken
        );
        return candidates.Count == 0 ? null : candidates[0];
    }
}
