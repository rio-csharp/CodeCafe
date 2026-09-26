using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.ListTrash;

public sealed record ListTrashQuery(int? Page, int? PageSize)
    : IQuery<Result<PagedResult<TrashEntryDto>>>;
