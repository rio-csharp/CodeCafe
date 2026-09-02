using FluentValidation;

namespace CodeCafe.Application.Notes.Commands.ChangeNotebookSlug;

public sealed class ChangeNotebookSlugCommandValidator : AbstractValidator<ChangeNotebookSlugCommand>
{
    public ChangeNotebookSlugCommandValidator()
    {
        RuleFor(command => command.Slug)
            .Must(NotebookInput.IsValidSlug)
            .WithMessage("Slug must be 8-180 characters of lowercase letters, digits, and dashes.");
    }
}
