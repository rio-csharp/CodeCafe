using FluentValidation;

namespace CodeCafe.Application.Notes.Commands.UpdateNotebook;

public sealed class UpdateNotebookCommandValidator : AbstractValidator<UpdateNotebookCommand>
{
    public UpdateNotebookCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(command => command.Description)
            .MaximumLength(1000);

        RuleFor(command => command.Visibility)
            .NotEmpty()
            .Must(value => NotebookInput.TryParseVisibility(value, out _))
            .WithMessage("Visibility must be public, private, or unlisted.");
    }
}
