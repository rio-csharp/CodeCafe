using CodeCafe.Application.Auth.UpdateProfile;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.UpdateProfile;

public sealed class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, Result<AuthUserDto>>
{
    public Task<Result<AuthUserDto>> Handle(UpdateProfileCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
