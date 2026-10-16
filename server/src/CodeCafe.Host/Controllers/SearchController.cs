using CodeCafe.Application.Common;
using CodeCafe.Application.Pages.SearchAllPages;
using CodeCafe.Application.Pages.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Controllers;

[ApiController]
[Route("api")]
[Tags("Search")]
public sealed class SearchController(ISender sender) : ControllerBase
{
    [HttpGet("search")]
    public Task<Result<CursorPage<PageSearchHitDto>>> Search(string q, string? cursor, int? pageSize, CancellationToken cancellationToken)
        => sender.Send(new SearchAllPagesQuery(q, cursor, pageSize), cancellationToken);
}
