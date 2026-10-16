using CodeCafe.Domain.Pages;
using FluentValidation;

namespace CodeCafe.Application.Pages.UpdatePage;

public sealed class UpdatePageCommandValidator : AbstractValidator<UpdatePageCommand>
{
    public UpdatePageCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(Page.MaxTitleLength)
            .When(command => command.Title is not null);
    }
}
