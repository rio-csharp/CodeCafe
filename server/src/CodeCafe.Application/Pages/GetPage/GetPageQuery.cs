using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.GetPage;

public sealed record GetPageQuery(Guid PageId) : IQuery<Result<PageDetailsDto>>;
