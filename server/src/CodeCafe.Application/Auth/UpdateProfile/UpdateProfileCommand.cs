using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.UpdateProfile;

public sealed record UpdateProfileCommand(string DisplayName) : ICommand<Result<AuthUserDto>>;
