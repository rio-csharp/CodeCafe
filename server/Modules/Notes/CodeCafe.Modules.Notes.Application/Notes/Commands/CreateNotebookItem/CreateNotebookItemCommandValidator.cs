using FluentValidation;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.CreateNotebookItem;

public sealed class CreateNotebookItemCommandValidator : AbstractValidator<CreateNotebookItemCommand>
{
    public CreateNotebookItemCommandValidator()
    {
        RuleFor(command => command.Type)
            .NotEmpty()
            .Must(value => NotebookInput.TryParseItemType(value, out _))
            .WithMessage("Item type must be folder or page.");

        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(160);
    }
}
