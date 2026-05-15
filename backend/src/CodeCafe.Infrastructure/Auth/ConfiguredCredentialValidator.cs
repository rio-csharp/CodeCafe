using CodeCafe.Application.Auth;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace CodeCafe.Infrastructure.Auth;

public sealed class ConfiguredCredentialValidator(IOptions<ConfiguredLoginOptions> configuredLoginOptions)
    : ICredentialValidator
{
    public string? Validate(string username, string password)
    {
        var configuredCredentials = configuredLoginOptions.Value;
        var configuredUsernameBytes = Encoding.UTF8.GetBytes(configuredCredentials.Username);
        var requestUsernameBytes = Encoding.UTF8.GetBytes(username);
        var configuredPasswordBytes = Encoding.UTF8.GetBytes(configuredCredentials.Password);
        var requestPasswordBytes = Encoding.UTF8.GetBytes(password);

        return FixedTimeEquals(configuredUsernameBytes, requestUsernameBytes)
            && FixedTimeEquals(configuredPasswordBytes, requestPasswordBytes)
                ? configuredCredentials.Username
                : null;
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
