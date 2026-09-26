using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.GetPageByPath;

public sealed record GetPageByPathQuery(string NotebookIdOrSlug, string Path, string? AccessCode = null)
    : IQuery<Result<PageDetailsDto>>;
