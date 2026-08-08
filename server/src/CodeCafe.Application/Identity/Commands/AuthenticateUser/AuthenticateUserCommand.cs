using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Identity.Commands.AuthenticateUser;

public sealed record AuthenticateUserCommand(
    string Email,
    string Password,
    string? ClientIp) : ICommand<AuthResult<AuthUserModel>>;
