using FluentValidation;

namespace CodeCafe.Application.Notebooks.PrepareNotebookImport;

public sealed class PrepareNotebookImportCommandValidator : AbstractValidator<PrepareNotebookImportCommand>
{
    public PrepareNotebookImportCommandValidator()
    {
        RuleFor(command => command.FileName).NotEmpty();
        RuleFor(command => command.MaxBytes).GreaterThan(0);
    }
}
