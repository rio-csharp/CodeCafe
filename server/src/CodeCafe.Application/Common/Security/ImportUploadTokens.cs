namespace CodeCafe.Application.Common.Security;

// The temporary credential an authenticated MCP call hands to the HTTP upload hop. It is a
// signed JWT with a distinct audience, so it cannot be replayed as an access token or a
// personal access token, and it carries everything the upload endpoint needs.
public static class ImportUploadTokens
{
    public const string Prefix = "ccu_";

    public const string Audience = "codecafe-import";
}

public enum ImportUploadKind
{
    Page,
    Notebook,
}

// The target the grant is allowed to write: one notebook, one parent path, one file. MaxBytes
// is enforced again on the upload request, so a client cannot widen its own limit.
public sealed record ImportUploadGrant(
    ImportUploadKind Kind,
    Guid UserId,
    Guid? NotebookId,
    string? ParentPath,
    string FileName,
    int MaxBytes
);

public sealed record ImportUploadToken(string Value, DateTimeOffset ExpiresAtUtc)
{
    // Records get logged verbatim more often than they should; keep the secret out of ToString().
    public override string ToString() => $"ImportUploadToken(Value = ***, ExpiresAtUtc = {ExpiresAtUtc:O})";
}

public interface IImportUploadTokenService
{
    ImportUploadToken Issue(ImportUploadGrant grant);

    Task<ImportUploadGrant?> ValidateAsync(string token, CancellationToken cancellationToken);
}
