using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

using CodeCafe.Application.Pages.Shared;
namespace CodeCafe.Application.Pages.CreatePage;

public sealed class CreatePageCommandHandler : ICommandHandler<CreatePageCommand, Result<CodeCafe.Application.Pages.Shared.PageDetailsDto>>
{
    public Task<Result<CodeCafe.Application.Pages.Shared.PageDetailsDto>> Handle(CreatePageCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
