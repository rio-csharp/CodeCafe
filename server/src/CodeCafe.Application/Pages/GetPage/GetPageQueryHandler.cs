using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;
namespace CodeCafe.Application.Pages.GetPage;

public sealed class GetPageQueryHandler : IQueryHandler<GetPageQuery, Result<PageDetailsDto>>
{
    public Task<Result<PageDetailsDto>> Handle(GetPageQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
