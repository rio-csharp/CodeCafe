using FluentValidation;

namespace CodeCafe.Application.Pages.PreparePageImport;

public sealed class PreparePageImportCommandValidator : AbstractValidator<PreparePageImportCommand>
{
    public PreparePageImportCommandValidator()
    {
        RuleFor(command => command.NotebookIdOrSlug).NotEmpty();
        RuleFor(command => command.FileName).NotEmpty();
        RuleFor(command => command.MaxBytes).GreaterThan(0);
    }
}
