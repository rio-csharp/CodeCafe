using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Commands;
namespace CodeCafe.Application.Pages.Handlers;

public sealed class CreatePageCommandHandler : ICommandHandler<CreatePageCommand, Result<Pages.Models.PageDetailsDto>>
{
    public Task<Result<Pages.Models.PageDetailsDto>> Handle(CreatePageCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
