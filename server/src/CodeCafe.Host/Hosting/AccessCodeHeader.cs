namespace CodeCafe.Host.Hosting;

// Access codes are credentials, so they travel in a header: query strings end up in browser
// history, Referer headers, and access logs. MCP tools are unaffected — they pass the code
// straight into the query object.
internal static class AccessCodeHeader
{
    public const string Name = "X-CodeCafe-Access-Code";

    public static string? Read(HttpRequest request) =>
        request.Headers.TryGetValue(Name, out var values) && !string.IsNullOrWhiteSpace(values)
            ? values.ToString()
            : null;
}
