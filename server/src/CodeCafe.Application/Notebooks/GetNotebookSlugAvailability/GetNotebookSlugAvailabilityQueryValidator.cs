using FluentValidation;

namespace CodeCafe.Application.Notebooks.GetNotebookSlugAvailability;

public sealed class GetNotebookSlugAvailabilityQueryValidator : AbstractValidator<GetNotebookSlugAvailabilityQuery>
{
    public GetNotebookSlugAvailabilityQueryValidator()
    {
        RuleFor(query => query.Slug)
            .Must(slug => slug is not null && NotebookSlug.IsValid(slug))
            .WithMessage("Slug may only contain letters, digits, and single hyphens in between.");
    }
}
