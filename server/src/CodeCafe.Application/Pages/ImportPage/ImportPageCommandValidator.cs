using FluentValidation;

namespace CodeCafe.Application.Pages.ImportPage;

public sealed class ImportPageCommandValidator : AbstractValidator<ImportPageCommand>
{
    public ImportPageCommandValidator()
    {
        RuleFor(command => command.NotebookIdOrSlug).NotEmpty();
        RuleFor(command => command.Export).NotNull();
        RuleFor(command => command.Export.FileName).NotEmpty();
        RuleFor(command => command.Export.Markdown).NotEmpty();
    }
}
