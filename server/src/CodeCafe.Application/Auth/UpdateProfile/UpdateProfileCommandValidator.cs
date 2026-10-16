using CodeCafe.Domain.Identity;
using FluentValidation;

namespace CodeCafe.Application.Auth.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(User.MaxDisplayNameLength);
    }
}
