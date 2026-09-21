using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;
namespace CodeCafe.Application.Pages.GetPageByPath;

public sealed class GetPageByPathQueryHandler : IQueryHandler<GetPageByPathQuery, Result<PageDetailsDto>>
{
    public Task<Result<PageDetailsDto>> Handle(GetPageByPathQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
