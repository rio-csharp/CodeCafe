using CodeCafe.Application.Common.Security;

namespace CodeCafe.Host.Mcp;

// The upload instructions an MCP tool hands back: a URL, the scoped token, and a command the
// model can run with the local file path. The token is the only credential in the response.
internal static class ImportUploadResponse
{
    public static object Build(
        ImportUploadGrant grant,
        IImportUploadTokenService tokens,
        HttpContext? httpContext,
        string endpoint
    )
    {
        var token = tokens.Issue(grant);
        var url = $"{ImportGuidance.Origin(httpContext)}/api/import-uploads/{endpoint}";

        return new
        {
            uploadUrl = url,
            uploadToken = token.Value,
            expiresAt = token.ExpiresAtUtc,
            maxUploadBytes = grant.MaxBytes,
            method = "POST",
            formField = "file",
            example = $"curl.exe -sS -X POST -H \"Authorization: Bearer {token.Value}\" -F \"file=@C:/path/to/file.md\" {url}",
        };
    }
}
