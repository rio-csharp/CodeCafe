using CodeCafe.Domain.Identity;
using FluentValidation;

namespace CodeCafe.Application.Pages.SharePage;

public sealed class SharePageCommandValidator : AbstractValidator<SharePageCommand>
{
    public SharePageCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(User.MaxEmailLength);
    }
}
