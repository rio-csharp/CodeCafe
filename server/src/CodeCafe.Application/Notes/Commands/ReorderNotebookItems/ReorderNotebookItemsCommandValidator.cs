using FluentValidation;

namespace CodeCafe.Application.Notes.Commands.ReorderNotebookItems;

public sealed class ReorderNotebookItemsCommandValidator
    : AbstractValidator<ReorderNotebookItemsCommand>
{
    public ReorderNotebookItemsCommandValidator()
    {
        RuleFor(command => command.Items)
            .NotNull()
            .Must(items => items.Count > 0)
            .WithMessage("At least one reorder item is required.");
    }
}
