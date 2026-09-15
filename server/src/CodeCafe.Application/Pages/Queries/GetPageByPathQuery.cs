using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Models;

namespace CodeCafe.Application.Pages.Queries;

public sealed record GetPageByPathQuery(string NotebookIdOrSlug, string Path)
    : IQuery<Result<PageDetailsDto>>;
