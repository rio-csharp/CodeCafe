using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Auth.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    bool RegistrationEnabled,
    string Email,
    string Password,
    string DisplayName) : ICommand<AuthResult<AuthUserModel>>;
