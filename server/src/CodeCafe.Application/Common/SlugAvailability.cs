using System.Globalization;

namespace CodeCafe.Application.Common;

internal static class SlugAvailability
{
    // Random suffixes can collide, so attempts are bounded instead of looping forever.
    private const int MaxAttempts = 50;

    // Available variants of the base slug, best first. The base slug itself is the first candidate
    // when it is free, otherwise every candidate carries a random numeric suffix.
    public static async Task<IReadOnlyList<string>> FindAvailableAsync(
        string baseSlug,
        int maxLength,
        int count,
        Func<string, CancellationToken, Task<bool>> isTaken,
        CancellationToken cancellationToken
    )
    {
        var free = new List<string>(count);

        if (!await isTaken(baseSlug, cancellationToken))
        {
            free.Add(baseSlug);
        }

        for (var attempt = 0; attempt < MaxAttempts && free.Count < count; attempt++)
        {
            var candidate = Slug.WithSuffix(baseSlug, NextSuffix(), maxLength);
            if (free.Contains(candidate))
            {
                continue;
            }

            if (!await isTaken(candidate, cancellationToken))
            {
                free.Add(candidate);
            }
        }

        return free;
    }

    // Four digits: short enough to stay readable, wide enough that a handful of draws rarely
    // collide. Invariant digits so the slug never picks up a locale's numeral shapes.
    // Capacity: 9000 variants per base slug, shared globally for notebooks (unique slug index).
    // Fallback titles all draw from the same pool ("notebook-1234"); with 50 bounded attempts
    // this only degrades near exhaustion (~8000 taken). If that ever becomes a real limit,
    // widen the range here (e.g. six digits) - WithSuffix already makes room for longer suffixes.
    private static string NextSuffix() => Random.Shared.Next(1000, 10000).ToString(CultureInfo.InvariantCulture);
}
