namespace CodeCafe.DbSync.Infrastructure;

internal static class ConnectionStringEditor
{
    public static string WithHostAndPort(string connectionString, string host, int port)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in parts)
        {
            var separator = part.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            values[part[..separator]] = part[(separator + 1)..];
        }

        values["Host"] = host;
        values["Port"] = port.ToString();

        return string.Join(';', values.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    public static string MaskPassword(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(';', parts.Select(part =>
            part.StartsWith("Password=", StringComparison.OrdinalIgnoreCase)
                ? "Password=<masked>"
                : part));
    }
}
