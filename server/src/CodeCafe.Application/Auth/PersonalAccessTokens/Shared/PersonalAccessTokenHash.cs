using System.Security.Cryptography;
using System.Text;

namespace CodeCafe.Application.Auth.PersonalAccessTokens.Shared;

// Same scheme as refresh tokens: SHA-256 over the token, hex-encoded, and only the hash is
// ever stored. The authentication handler recomputes it for lookup, so both sides share this.
public static class PersonalAccessTokenHash
{
    public const string Prefix = "ccp_";

    public static bool IsPersonalAccessToken(string token)
        => token.StartsWith(Prefix, StringComparison.Ordinal);

    public static string Compute(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
