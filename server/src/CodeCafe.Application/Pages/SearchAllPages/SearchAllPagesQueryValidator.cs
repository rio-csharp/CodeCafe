using FluentValidation;

namespace CodeCafe.Application.Pages.SearchAllPages;

public sealed class SearchAllPagesQueryValidator : AbstractValidator<SearchAllPagesQuery>
{
    public const int MaxQueryLength = 200;

    public SearchAllPagesQueryValidator()
    {
        RuleFor(query => query.Query).NotEmpty().MaximumLength(MaxQueryLength);
    }
}
