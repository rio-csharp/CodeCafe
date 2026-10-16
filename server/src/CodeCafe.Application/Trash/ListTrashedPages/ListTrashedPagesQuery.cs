using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.ListTrashedPages;

public sealed record ListTrashedPagesQuery(string NotebookIdOrSlug, int? Page, int? PageSize)
    : IQuery<Result<PagedResult<TrashedPageEntryDto>>>;
