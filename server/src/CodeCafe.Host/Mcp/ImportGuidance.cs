using System.Text;
using CodeCafe.Application.Common;
using CodeCafe.Host.Hosting;
using ModelContextProtocol.Protocol;

namespace CodeCafe.Host.Mcp;

internal sealed record ImportEndpoint(string Method, string Url, string Body, string Example);

// Everything a client needs to move content over HTTP instead of through the model's context.
// The upload endpoints take a short-lived token issued by codecafe_prepare_*_import; the MCP
// client's authenticated bearer is deliberately not part of this flow.
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
        const string authorization =
            "Authorization: Bearer <temporary upload token from codecafe_prepare_page_import or codecafe_prepare_notebook_import>";

        return new ImportRecipe(
            authorization,
            options.MaxInlineContentBytes,
            RequestLimits.ImportBytes,
            new ImportEndpoint(
                "POST",
                $"{origin}/api/import-uploads/notebooks",
                "multipart/form-data with a `file` field; the token binds the file name",
                $"curl.exe -sS -X POST -H '{authorization}' -F file=@notes.md {origin}/api/import-uploads/notebooks"
            ),
            new ImportEndpoint(
                "POST",
                $"{origin}/api/import-uploads/pages",
                "multipart/form-data with a `file` field; the token binds the notebook, parent path, and file name",
                $"curl.exe -sS -X POST -H '{authorization}' -F file=@note.md {origin}/api/import-uploads/pages"
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
                + "Call codecafe_prepare_page_import (pages) or codecafe_prepare_notebook_import "
                + "(notebooks) to get a short-lived upload token, POST the file to the returned URL, "
                + "then report the result; do not pass the content through a tool argument.",
            ErrorKind.Validation
        );

        // The recipe rides along in structured content so the way out is machine-readable rather
        // than something the model has to reassemble from the message above.
        return McpToolResults.Failure(error, new { recipe });
    }

    internal static string Origin(HttpContext? httpContext)
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
