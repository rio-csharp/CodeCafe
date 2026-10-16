using FluentValidation;

namespace CodeCafe.Application.Pages.ChangePageSlug;

public sealed class ChangePageSlugCommandValidator : AbstractValidator<ChangePageSlugCommand>
{
    public ChangePageSlugCommandValidator()
    {
        RuleFor(command => command.Slug)
            // An absent slug means "derive one from the title", so only a provided slug is checked.
            // Validate the normalized form: the handler trims and lowercases anyway, so a padded
            // but otherwise valid slug should pass instead of 400ing on the whitespace.
            .Must(slug => slug is not null && PageSlug.IsValid(PageSlug.Normalize(slug)))
            .WithMessage("Slug may only contain letters, digits, and single hyphens in between.")
            .When(command => !string.IsNullOrWhiteSpace(command.Slug));
    }
}
