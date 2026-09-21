using System.Globalization;
using CodeCafe.Application.Notebooks.Abstractions;

namespace CodeCafe.Application.Notebooks;

internal static class NotebookSlugAvailability
{
    // Random suffixes can collide, so attempts are bounded instead of looping forever.
    private const int MaxAttempts = 50;

    // Available variants of the base slug, best first. The base slug itself is the first candidate
    // when it is free, otherwise every candidate carries a random numeric suffix.
    public static async Task<IReadOnlyList<string>> FindAvailableAsync(
        string baseSlug,
        int count,
        INotebookRepository notebooks,
        CancellationToken cancellationToken
    )
    {
        var free = new List<string>(count);

        if (await notebooks.FindBySlugAsync(baseSlug, cancellationToken) is null)
        {
            free.Add(baseSlug);
        }

        for (var attempt = 0; attempt < MaxAttempts && free.Count < count; attempt++)
        {
            var candidate = NotebookSlug.WithSuffix(baseSlug, NextSuffix());
            if (free.Contains(candidate))
            {
                continue;
            }

            if (await notebooks.FindBySlugAsync(candidate, cancellationToken) is null)
            {
                free.Add(candidate);
            }
        }

        return free;
    }

    // Four digits: short enough to stay readable, wide enough that a handful of draws rarely
    // collide. Invariant digits so the slug never picks up a locale's numeral shapes.
    private static string NextSuffix() => Random.Shared.Next(1000, 10000).ToString(CultureInfo.InvariantCulture);
}
