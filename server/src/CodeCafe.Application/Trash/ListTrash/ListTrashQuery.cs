using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.ListTrash;

public sealed record ListTrashQuery(string? Cursor, int? PageSize)
    : IQuery<Result<CursorPage<TrashEntryDto>>>;
