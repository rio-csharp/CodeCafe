using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Commands;
using CodeCafe.Application.Pages.Models;

namespace CodeCafe.Application.Pages.Handlers;

public sealed class UpdatePageCommandHandler : ICommandHandler<UpdatePageCommand, Result<PageDetailsDto>>
{
    public Task<Result<PageDetailsDto>> Handle(UpdatePageCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
