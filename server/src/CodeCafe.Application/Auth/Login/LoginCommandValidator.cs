using FluentValidation;

namespace CodeCafe.Application.Auth.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();

        RuleFor(command => command.Password).NotEmpty();
    }
}
