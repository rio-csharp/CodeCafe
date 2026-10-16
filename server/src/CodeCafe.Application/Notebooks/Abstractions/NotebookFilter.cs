using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.Abstractions;

// Everything a notebook listing can be narrowed by, as one input for both the count and the page,
// so the two can never end up filtered differently.
public sealed record NotebookFilter(
    string? Tag,
    bool? IsFavorite,
    NotebookVisibility? Visibility,
    string? Search);
