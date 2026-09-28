using CodeCafe.Application.Auth;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;

namespace CodeCafe.Application.Trash.ListTrash;

public sealed class ListTrashQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages
) : IQueryHandler<ListTrashQuery, Result<PagedResult<TrashEntryDto>>>
{
    // Trash lists are owner-only, so a short list of sane bounds is enough.
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<TrashEntryDto>>> Handle(ListTrashQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        if (userId is null)
        {
            return Result.Failure<PagedResult<TrashEntryDto>>(AuthErrors.UserNotFound);
        }

        var page = Math.Max(query.Page ?? 1, 1);
        var pageSize = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);

        var trashed = await notebooks.ListTrashedAsync(userId.Value, (page - 1) * pageSize, pageSize, cancellationToken);
        var totalCount = await notebooks.CountTrashedAsync(userId.Value, cancellationToken);

        var pageCounts = await pages.CountByNotebooksAsync(
            trashed.Select(notebook => notebook.Id).ToList(),
            cancellationToken
        );

        var items = trashed
            .Select(notebook => new TrashEntryDto(
                notebook.Id,
                notebook.Title,
                pageCounts.GetValueOrDefault(notebook.Id),
                notebook.DeletedAtUtc!.Value
            ))
            .ToList();

        return Result.Success(new PagedResult<TrashEntryDto>(items, page, pageSize, totalCount));
    }
}
