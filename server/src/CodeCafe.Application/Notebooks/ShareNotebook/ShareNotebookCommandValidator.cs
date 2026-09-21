using CodeCafe.Domain.Identity;
using FluentValidation;

namespace CodeCafe.Application.Notebooks.ShareNotebook;

public sealed class ShareNotebookCommandValidator : AbstractValidator<ShareNotebookCommand>
{
    public ShareNotebookCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(User.MaxEmailLength);
    }
}
