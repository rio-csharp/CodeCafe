using FluentValidation;

namespace CodeCafe.Application.Notebooks.ChangeNotebookSlug;

public sealed class ChangeNotebookSlugCommandValidator : AbstractValidator<ChangeNotebookSlugCommand>
{
    public ChangeNotebookSlugCommandValidator()
    {
        RuleFor(command => command.NewSlug)
            .Must(slug => slug is not null && NotebookSlug.IsValid(slug))
            .WithMessage("Slug may only contain letters, digits, and single hyphens in between.");
    }
}
