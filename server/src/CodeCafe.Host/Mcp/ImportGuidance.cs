using System.Text;
using CodeCafe.Application.Common;
using CodeCafe.Host.Hosting;
using ModelContextProtocol.Protocol;

namespace CodeCafe.Host.Mcp;

internal sealed record ImportEndpoint(string Method, string Url, string Body, string Example);

// Everything a client needs to move content over HTTP instead of through the model's context:
// the import endpoints already accept the same markdown the MCP tools do, so an oversized
// payload is never actually stuck, it just has to change transport.
internal sealed record ImportRecipe(
    string Authorization,
    int InlineContentLimitBytes,
    int MaxUploadBytes,
    ImportEndpoint Notebook,
    ImportEndpoint Page
);

internal static class ImportGuidance
{
    // Deliberately carries the absolute origin: the client has to be able to run the example
    // as-is, and the host is deployment-specific.
    public static ImportRecipe Recipe(HttpContext? httpContext, McpOptions options)
    {
        var origin = Origin(httpContext);
        const string authorization = "Authorization: Bearer <the token this MCP client authenticates with>";

        return new ImportRecipe(
            authorization,
            options.MaxInlineContentBytes,
            RequestLimits.ImportBytes,
            new ImportEndpoint(
                "POST",
                $"{origin}/api/notebooks/import/upload",
                "multipart/form-data with a `fileName` field and a `file` field holding the markdown",
                $"curl -sS -X POST -H '{authorization}' -F fileName=notes.md -F file=@notes.md {origin}/api/notebooks/import/upload"
            ),
            new ImportEndpoint(
                "POST",
                $"{origin}/api/notebooks/{{notebookIdOrSlug}}/pages/import/upload",
                "multipart/form-data with `fileName`, `file`, and an optional `parentPath`",
                $"curl -sS -X POST -H '{authorization}' -F fileName=note.md -F file=@note.md {origin}/api/notebooks/my-notebook/pages/import/upload"
            )
        );
    }

    // Returns a refusal the model can act on, or null when the payload may travel inline.
    public static CallToolResult? TooLarge(string content, ImportRecipe recipe)
    {
        var bytes = Encoding.UTF8.GetByteCount(content);
        if (bytes <= recipe.InlineContentLimitBytes)
        {
            return null;
        }

        var error = new Error(
            "content_too_large_for_tool_argument",
            $"{bytes} bytes of markdown exceeds the {recipe.InlineContentLimitBytes} byte inline limit. "
                + "Upload the file to the HTTP endpoint below with the bearer token you already use, "
                + "then report the result; do not pass the content through a tool argument.",
            ErrorKind.Validation
        );

        // The recipe rides along in structured content so the way out is machine-readable rather
        // than something the model has to reassemble from the message above.
        return McpToolResults.Failure(error, new { recipe });
    }

    private static string Origin(HttpContext? httpContext)
    {
        if (httpContext?.Request.Host is not { HasValue: true } host)
        {
            // Only reachable outside a request (a direct service call in a test), where there is
            // no origin to advertise; the example still shows the shape.
            return "https://<api-host>";
        }

        // Behind the ingress the forwarded-headers middleware has already replaced scheme and
        // host with the public ones, so this is the URL the caller can reach.
        return $"{httpContext.Request.Scheme}://{host.Value}";
    }
}
