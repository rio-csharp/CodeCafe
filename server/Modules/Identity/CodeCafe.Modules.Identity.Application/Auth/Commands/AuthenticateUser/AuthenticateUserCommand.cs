using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Identity.Application.Auth.Commands.AuthenticateUser;

public sealed record AuthenticateUserCommand(
    string Email,
    string Password,
    string? ClientIp) : ICommand<AuthResult<AuthUserModel>>;
