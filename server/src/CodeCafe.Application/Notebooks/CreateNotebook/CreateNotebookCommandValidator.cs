using CodeCafe.Domain.Notebooks;
using FluentValidation;

namespace CodeCafe.Application.Notebooks.CreateNotebook;

public sealed class CreateNotebookCommandValidator : AbstractValidator<CreateNotebookCommand>
{
    public CreateNotebookCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(Notebook.MaxTitleLength);

        RuleFor(command => command.Description).MaximumLength(Notebook.MaxDescriptionLength);

        RuleFor(command => command.Slug)
            .Must(slug => slug is null || NotebookSlug.IsValid(slug))
            .WithMessage("Slug may only contain letters, digits, and single hyphens in between.");
    }
}
