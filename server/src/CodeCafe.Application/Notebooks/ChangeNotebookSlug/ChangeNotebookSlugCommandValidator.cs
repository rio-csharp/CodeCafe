using FluentValidation;

namespace CodeCafe.Application.Notebooks.ChangeNotebookSlug;

public sealed class ChangeNotebookSlugCommandValidator : AbstractValidator<ChangeNotebookSlugCommand>
{
    public ChangeNotebookSlugCommandValidator()
    {
        RuleFor(command => command.NewSlug)
            // Validate the normalized form: the handler trims and lowercases anyway, so a
            // padded but otherwise valid slug should pass instead of 400ing on the whitespace.
            .Must(slug => slug is not null && NotebookSlug.IsValid(NotebookSlug.Normalize(slug)))
            .WithMessage("Slug may only contain letters, digits, and single hyphens in between.");
    }
}
