using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Trash.ListTrash;

public sealed class ListTrashQueryHandler : IQueryHandler<ListTrashQuery, Result<CursorPage<TrashEntryDto>>>
{
    public Task<Result<CursorPage<TrashEntryDto>>> Handle(ListTrashQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
