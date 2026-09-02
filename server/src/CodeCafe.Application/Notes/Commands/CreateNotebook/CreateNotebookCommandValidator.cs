using FluentValidation;

namespace CodeCafe.Application.Notes.Commands.CreateNotebook;

public sealed class CreateNotebookCommandValidator : AbstractValidator<CreateNotebookCommand>
{
    public CreateNotebookCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(160);

        RuleFor(command => command.Description).MaximumLength(1000);

        RuleFor(command => command.Visibility)
            .Must(value => NotebookInput.TryParseVisibility(value, out _))
            .WithMessage("Visibility must be public, private, or unlisted.");

        RuleFor(command => command.Slug)
            .Must(NotebookInput.IsValidSlug)
            .When(command => !string.IsNullOrWhiteSpace(command.Slug))
            .WithMessage("Slug must be 8-180 characters of lowercase letters, digits, and dashes.");
    }
}
