using FluentValidation;

namespace CodeCafe.Application.Notebooks.SetNotebookAccessCode;

public sealed class SetNotebookAccessCodeCommandValidator : AbstractValidator<SetNotebookAccessCodeCommand>
{
    public SetNotebookAccessCodeCommandValidator()
    {
        // Null clears the access code; setting one requires a usable length.
        RuleFor(command => command.AccessCode)
            .NotEmpty()
            .MinimumLength(4)
            .MaximumLength(64)
            .When(command => command.AccessCode is not null);
    }
}
