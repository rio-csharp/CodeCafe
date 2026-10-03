using System.Globalization;
using System.Text;

namespace CodeCafe.Application.Revisions.Shared;

// Keyset cursor for the revision reads: base64 of "{createdAtUtc:o}|{id}", opaque to callers.
// Same shape as the search cursor, kept local because nothing shares cursor formats yet.
public static class RevisionCursor
{
    public static string Encode(DateTimeOffset createdAtUtc, Guid id)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{createdAtUtc:o}|{id}"));

    public static bool TryDecode(string cursor, out DateTimeOffset createdAtUtc, out Guid id)
    {
        createdAtUtc = default;
        id = default;

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        }
        catch (FormatException)
        {
            return false;
        }

        var parts = decoded.Split('|');
        return parts.Length == 2
            && DateTimeOffset.TryParseExact(
                parts[0],
                "o",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out createdAtUtc
            )
            && Guid.TryParse(parts[1], out id);
    }
}
