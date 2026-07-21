using FluentValidation;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.UpdateNotebookItem;

public sealed class UpdateNotebookItemCommandValidator : AbstractValidator<UpdateNotebookItemCommand>
{
    public UpdateNotebookItemCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(command => command.ParentId)
            .Must(NotebookInput.IsOptionalGuid)
            .WithMessage("ParentId must be a GUID or null.");
    }
}
