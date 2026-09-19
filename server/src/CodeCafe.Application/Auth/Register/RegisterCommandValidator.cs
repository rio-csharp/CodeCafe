using CodeCafe.Domain.Identity;
using FluentValidation;

namespace CodeCafe.Application.Auth.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            // Matches the users.email column length so oversized input is rejected as a 400
            // instead of failing the insert with a 500.
            .MaximumLength(User.MaxEmailLength);

        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(128);

        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(User.MaxDisplayNameLength);
    }
}
