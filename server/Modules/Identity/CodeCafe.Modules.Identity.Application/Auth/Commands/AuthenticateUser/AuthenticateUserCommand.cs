using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Auth.Commands.AuthenticateUser;

public sealed record AuthenticateUserCommand(
    string Email,
    string Password) : ICommand<AuthResult<AuthUserModel>>;
