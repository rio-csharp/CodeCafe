using CodeCafe.Domain.Pages;
using FluentValidation;

namespace CodeCafe.Application.Pages.CreatePage;

public sealed class CreatePageCommandValidator : AbstractValidator<CreatePageCommand>
{
    public CreatePageCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(Page.MaxTitleLength);
    }
}
