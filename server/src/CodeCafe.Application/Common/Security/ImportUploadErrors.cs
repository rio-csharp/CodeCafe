using CodeCafe.Application.Common;

namespace CodeCafe.Application.Common.Security;

public static class ImportUploadErrors
{
    public static readonly Error WrongKind = new(
        "import_upload_wrong_kind",
        "This upload token is not valid for that import endpoint.",
        ErrorKind.Validation
    );

    // Reachable only if the endpoint runs without the upload authentication scheme, which
    // would be a wiring bug; the message still avoids confirming anything about the token.
    public static readonly Error MissingGrant = new(
        "import_upload_missing",
        "The upload token was not accepted.",
        ErrorKind.Unauthorized
    );

    public static readonly Error InvalidMaxBytes = new(
        "invalid_max_upload_bytes",
        "The upload size limit must be greater than zero.",
        ErrorKind.Validation
    );

    public static Error TooLarge(int maxBytes) => new(
        "upload_too_large",
        $"The uploaded file exceeds the {maxBytes} byte limit.",
        ErrorKind.Validation
    );
}
