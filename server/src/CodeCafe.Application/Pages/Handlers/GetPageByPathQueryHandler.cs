using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Models;
using CodeCafe.Application.Pages.Queries;
namespace CodeCafe.Application.Pages.Handlers;

public sealed class GetPageByPathQueryHandler : IQueryHandler<GetPageByPathQuery, Result<PageDetailsDto>>
{
    public Task<Result<PageDetailsDto>> Handle(GetPageByPathQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
