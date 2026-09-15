using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Models;
using CodeCafe.Application.Pages.Queries;
namespace CodeCafe.Application.Pages.Handlers;

public sealed class GetPageQueryHandler : IQueryHandler<GetPageQuery, Result<PageDetailsDto>>
{
    public Task<Result<PageDetailsDto>> Handle(GetPageQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
