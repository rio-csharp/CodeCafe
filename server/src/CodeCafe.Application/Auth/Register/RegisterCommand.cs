using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Register;

// RegistrationEnabled is carried on the command rather than read from configuration, so the
// application layer stays free of IConfiguration and every caller has to state the policy it
// is acting under. The composition root supplies the deployment's value.
public sealed record RegisterCommand(
    string Email,
    string Password,
    string DisplayName,
    bool RegistrationEnabled
) : ICommand<Result<AuthSessionDto>>;
