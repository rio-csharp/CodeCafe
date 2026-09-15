using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Trash.Models;

namespace CodeCafe.Application.Trash.Queries;

public sealed record ListTrashQuery(string? Cursor, int? PageSize)
    : IQuery<Result<CursorPage<TrashEntryDto>>>;
