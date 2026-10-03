using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.ExportPage;

public sealed class ExportPageQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IBlockRepository blocks,
    IPasswordHasher passwordHasher
) : IQueryHandler<ExportPageQuery, Result<PageExportDto>>
{
    public async Task<Result<PageExportDto>> Handle(ExportPageQuery query, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireReadAsync(
            query.PageId,
            query.AccessCode,
            currentUserAccessor,
            notebooks,
            pages,
            passwordHasher,
            cancellationToken
        );
        if (context.Error is { } error)
        {
            return Result.Failure<PageExportDto>(error);
        }

        var page = context.Value!.Page;
        var pageBlocks = await blocks.ListByPageAsync(page.Id, cancellationToken);
        var markdown = MarkdownPageExporter.Render(page, pageBlocks);
        return Result.Success(new PageExportDto($"{page.Slug}.md", markdown));
    }
}
