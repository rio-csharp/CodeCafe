using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Identity.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    bool RegistrationEnabled,
    string Email,
    string Password,
    string DisplayName
) : ICommand<AuthResult<AuthUserModel>>;
