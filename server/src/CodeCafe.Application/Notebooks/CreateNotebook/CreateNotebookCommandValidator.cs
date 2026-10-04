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
            // Validate the normalized form: the handler trims and lowercases anyway, so a
            // padded but otherwise valid slug should pass instead of 400ing on the whitespace.
            .Must(slug => slug is null || NotebookSlug.IsValid(NotebookSlug.Normalize(slug)))
            .WithMessage("Slug may only contain letters, digits, and single hyphens in between.");
    }
}
