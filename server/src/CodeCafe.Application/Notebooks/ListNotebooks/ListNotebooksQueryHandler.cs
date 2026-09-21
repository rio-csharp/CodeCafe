using CodeCafe.Application.Auth;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;

namespace CodeCafe.Application.Notebooks.ListNotebooks;

public sealed class ListNotebooksQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks
) : IQueryHandler<ListNotebooksQuery, Result<PagedResult<NotebookSummaryDto>>>
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<NotebookSummaryDto>>> Handle(ListNotebooksQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        if (userId is null)
        {
            return Result.Failure<PagedResult<NotebookSummaryDto>>(AuthErrors.UserNotFound);
        }

        var page = Math.Max(query.Page ?? 1, 1);
        var pageSize = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        var tag = query.Tag?.Trim().ToLowerInvariant() is { Length: > 0 } normalizedTag ? normalizedTag : null;
        var sort = query.Sort ?? NotebookSort.UpdatedDesc;

        var totalCount = await notebooks.CountVisibleAsync(
            userId.Value,
            tag,
            query.IsFavorite,
            query.Visibility,
            cancellationToken
        );
        var fetched = await notebooks.ListVisibleAsync(
            userId.Value,
            tag,
            query.IsFavorite,
            query.Visibility,
            sort,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken
        );
        var favoriteIds = await notebooks.FindFavoriteIdsAsync(
            userId.Value,
            fetched.Select(notebook => notebook.Id).ToList(),
            cancellationToken
        );

        var items = fetched
            .Select(notebook => new NotebookSummaryDto(
                notebook.Id,
                notebook.Title,
                notebook.Slug,
                notebook.Visibility,
                favoriteIds.Contains(notebook.Id),
                notebook.Tags.ToList(),
                PageCount: 0,
                notebook.UpdatedAtUtc
            ))
            .ToList();

        return Result.Success(new PagedResult<NotebookSummaryDto>(items, page, pageSize, totalCount));
    }
}
