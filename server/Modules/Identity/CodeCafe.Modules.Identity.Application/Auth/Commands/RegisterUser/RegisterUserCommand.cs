using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Identity.Application.Auth.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    bool RegistrationEnabled,
    string Email,
    string Password,
    string DisplayName) : ICommand<AuthResult<AuthUserModel>>;
