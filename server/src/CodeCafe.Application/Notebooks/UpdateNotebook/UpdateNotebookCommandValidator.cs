using CodeCafe.Domain.Notebooks;
using FluentValidation;

namespace CodeCafe.Application.Notebooks.UpdateNotebook;

public sealed class UpdateNotebookCommandValidator : AbstractValidator<UpdateNotebookCommand>
{
    public UpdateNotebookCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(Notebook.MaxTitleLength)
            .When(command => command.Title is not null);

        RuleFor(command => command.Description)
            .MaximumLength(Notebook.MaxDescriptionLength)
            .When(command => command.Description is not null);
    }
}
