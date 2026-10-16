using CodeCafe.Domain.Notebooks;
using FluentValidation;

namespace CodeCafe.Application.Notebooks.SetNotebookTags;

public sealed class SetNotebookTagsCommandValidator : AbstractValidator<SetNotebookTagsCommand>
{
    public SetNotebookTagsCommandValidator()
    {
        RuleFor(command => command.Tags).NotNull();
        RuleFor(command => command.Tags.Count).LessThanOrEqualTo(Notebook.MaxTagCount);
        RuleForEach(command => command.Tags).NotEmpty().MaximumLength(Notebook.MaxTagLength);
    }
}
