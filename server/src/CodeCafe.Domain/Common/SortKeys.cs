using System.Text;

namespace CodeCafe.Domain.Common;

// LexoRank-style ordering keys over a base36 alphabet: Between always returns a key strictly
// between its bounds without touching any other row, so inserts and moves stay O(1) while reads
// get an index-friendly ORDER BY.
public static class SortKeys
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

    public static string First() => Between(null, null);

    // Re-dealing a whole level: fixed-width base36 ordinals spread a full stride apart, so
    // follow-up inserts start with room on both sides again. Width grows only when one digit's
    // 36 slots can no longer keep the keys apart.
    public static IReadOnlyList<string> EvenlySpaced(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var keys = new string[count];
        if (count == 0)
        {
            return keys;
        }

        var width = 1;
        long space = Alphabet.Length;
        while (space < count + 1)
        {
            width++;
            space *= Alphabet.Length;
        }

        var stride = Math.Max(1, space / (count + 1));
        for (var index = 0; index < count; index++)
        {
            keys[index] = ToBase36(stride * (index + 1), width);
        }

        return keys;
    }

    private static string ToBase36(long value, int width)
    {
        var characters = new char[width];
        for (var position = width - 1; position >= 0; position--)
        {
            characters[position] = Alphabet[(int)(value % Alphabet.Length)];
            value /= Alphabet.Length;
        }

        return new string(characters);
    }

    public static string Between(string? before, string? after)
    {
        if (before is not null && after is not null
            && string.CompareOrdinal(before, after) >= 0)
        {
            throw new ArgumentException("The lower bound must sort before the upper bound.");
        }

        var result = new StringBuilder();
        for (var position = 0; ; position++)
        {
            var low = before is not null && position < before.Length ? Alphabet.IndexOf(before[position]) : 0;
            var high = after is not null && position < after.Length ? Alphabet.IndexOf(after[position]) : Alphabet.Length;

            // Adjacent or equal digits leave no room here: keep the low digit and descend a level.
            if (high - low <= 1)
            {
                result.Append(Alphabet[low]);
                continue;
            }

            result.Append(Alphabet[(low + high) / 2]);
            return result.ToString();
        }
    }
}
