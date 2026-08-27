using FluentValidation;

namespace CodeCafe.Application.Identity.Commands.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            // Matches the AspNetUsers.Email column length so oversized input is
            // rejected as a 400 instead of failing the insert with a 500.
            .MaximumLength(256);

        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(128);

        RuleFor(command => command.DisplayName)
            .Must(displayName => !string.IsNullOrWhiteSpace(displayName))
            .WithMessage("Display name is required.")
            .MaximumLength(40);
    }
}
