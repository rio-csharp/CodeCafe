using FluentValidation;

namespace CodeCafe.Application.Notes.Commands.CreateNotebookItem;

public sealed class CreateNotebookItemCommandValidator : AbstractValidator<CreateNotebookItemCommand>
{
    public CreateNotebookItemCommandValidator()
    {
        RuleFor(command => command.Type)
            .NotEmpty();

        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(160);
    }
}
