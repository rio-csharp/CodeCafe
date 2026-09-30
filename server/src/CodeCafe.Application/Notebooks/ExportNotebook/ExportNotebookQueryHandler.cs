using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Notebooks.ExportNotebook;

public sealed class ExportNotebookQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IBlockRepository blocks,
    IPasswordHasher passwordHasher
) : IQueryHandler<ExportNotebookQuery, Result<NotebookExportDto>>
{
    public async Task<Result<NotebookExportDto>> Handle(
        ExportNotebookQuery query,
        CancellationToken cancellationToken
    )
    {
        var notebook = await notebooks.FindByIdOrSlugAsync(query.NotebookIdOrSlug, cancellationToken);
        if (notebook is null)
        {
            return Result.Failure<NotebookExportDto>(NotebookErrors.NotFound);
        }

        var accessError = NotebookAccess.CheckRead(
            notebook,
            currentUserAccessor.User?.Id,
            query.AccessCode,
            passwordHasher
        );
        if (accessError is not null)
        {
            return Result.Failure<NotebookExportDto>(accessError);
        }

        var notebookPages = await pages.ListByNotebookAsync(notebook.Id, cancellationToken);
        var pageIds = notebookPages.Select(page => page.Id).ToArray();
        var notebookBlocks = await blocks.ListByPageIdsAsync(pageIds, cancellationToken);
        var blocksByPage = notebookBlocks
            .GroupBy(block => block.PageId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Block>)group.ToList());

        var markdown = MarkdownExporter.Render(notebook, notebookPages, blocksByPage);
        return Result.Success(new NotebookExportDto($"{notebook.Slug}.md", markdown));
    }
}
