using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.GetNotebookSlugAvailability;

public sealed class GetNotebookSlugAvailabilityQueryHandler(INotebookRepository notebooks)
    : IQueryHandler<GetNotebookSlugAvailabilityQuery, Result<NotebookSlugAvailabilityDto>>
{
    private const int SuggestionCount = 3;

    public async Task<Result<NotebookSlugAvailabilityDto>> Handle(
        GetNotebookSlugAvailabilityQuery query,
        CancellationToken cancellationToken
    )
    {
        var slug = NotebookSlug.Normalize(query.Slug);

        if (await notebooks.FindBySlugAsync(slug, cancellationToken) is null)
        {
            return Result.Success(new NotebookSlugAvailabilityDto(slug, IsAvailable: true, Suggestions: []));
        }

        // A taken base slug never makes a candidate of its own, so every variant carries a suffix.
        var suggestions = await SlugAvailability.FindAvailableAsync(
            slug,
            Notebook.MaxSlugLength,
            SuggestionCount,
            async (candidate, ct) => await notebooks.FindBySlugAsync(candidate, ct) is not null,
            cancellationToken
        );

        return Result.Success(new NotebookSlugAvailabilityDto(slug, IsAvailable: false, Suggestions: suggestions));
    }
}
