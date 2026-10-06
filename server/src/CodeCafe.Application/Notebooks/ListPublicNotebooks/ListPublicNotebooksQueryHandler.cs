using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.ListNotebooks;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.ListPublicNotebooks;

public sealed class ListPublicNotebooksQueryHandler(
    INotebookRepository notebooks,
    IPageRepository pages,
    IUserRepository users
) : IQueryHandler<ListPublicNotebooksQuery, Result<PagedResult<NotebookSummaryDto>>>
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<NotebookSummaryDto>>> Handle(ListPublicNotebooksQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page ?? 1, 1);
        var pageSize = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        // The public catalog is exactly "visibility = Public": a null userId makes the
        // repository take the anonymous arm. Favorites don't exist without a user.
        var filter = new NotebookFilter(
            null,
            null,
            NotebookVisibility.Public,
            query.Search?.Trim() is { Length: > 0 } search ? search : null
        );
        var sort = query.Sort ?? NotebookSort.UpdatedDesc;

        var totalCount = await notebooks.CountVisibleAsync(null, filter, cancellationToken);
        var fetched = await notebooks.ListVisibleAsync(
            null,
            filter,
            sort,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken
        );
        var pageCounts = await pages.CountByNotebooksAsync(
            fetched.Select(notebook => notebook.Id).ToList(),
            cancellationToken
        );

        var ownerNames = await OwnerNames(fetched, users, cancellationToken);

        var items = fetched
            .Select(notebook => new NotebookSummaryDto(
                notebook.Id,
                notebook.Title,
                notebook.Description,
                notebook.Slug,
                notebook.Visibility,
                false,
                notebook.Tags.ToList(),
                pageCounts.GetValueOrDefault(notebook.Id),
                notebook.UpdatedAtUtc,
                ownerNames.GetValueOrDefault(notebook.OwnerId, string.Empty)
            ))
            .ToList();

        return Result.Success(new PagedResult<NotebookSummaryDto>(items, page, pageSize, totalCount));
    }

    internal static async Task<Dictionary<Guid, string>> OwnerNames(
        IReadOnlyList<Domain.Notebooks.Notebook> notebooks,
        IUserRepository users,
        CancellationToken cancellationToken
    )
    {
        var ownerIds = notebooks.Select(notebook => notebook.OwnerId).Distinct().ToList();
        var owners = await users.FindByIdsAsync(ownerIds, cancellationToken);
        return owners.ToDictionary(user => user.Id, user => user.DisplayName);
    }
}
