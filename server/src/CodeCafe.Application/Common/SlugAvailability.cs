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

    // 9000 variants per base slug, shared globally: the unique slug index means every notebook with
    // the same fallback title draws from one pool, so exhaustion is reachable — widen the range
    // here if it ever is. Invariant digits keep the slug ASCII whatever the server's locale.
    private static string NextSuffix() => Random.Shared.Next(1000, 10000).ToString(CultureInfo.InvariantCulture);
}
