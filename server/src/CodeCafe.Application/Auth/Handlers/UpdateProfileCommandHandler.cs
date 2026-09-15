using CodeCafe.Application.Auth.Commands;
using CodeCafe.Application.Auth.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Handlers;

public sealed class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, Result<AuthUserDto>>
{
    public Task<Result<AuthUserDto>> Handle(UpdateProfileCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
