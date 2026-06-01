using FluentValidation;

namespace CodeCafe.Application.Notes.Commands.UpdateNotebookItem;

public sealed class UpdateNotebookItemCommandValidator : AbstractValidator<UpdateNotebookItemCommand>
{
    public UpdateNotebookItemCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(160);
    }
}
