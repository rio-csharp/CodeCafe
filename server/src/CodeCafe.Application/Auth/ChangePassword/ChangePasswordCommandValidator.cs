using FluentValidation;

namespace CodeCafe.Application.Auth.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword).NotEmpty();

        // Same password policy as registration.
        RuleFor(command => command.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}
