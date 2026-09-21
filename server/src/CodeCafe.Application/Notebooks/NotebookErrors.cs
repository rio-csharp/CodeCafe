using CodeCafe.Application.Common;

namespace CodeCafe.Application.Notebooks;

public static class NotebookErrors
{
    // Also used for notebooks the caller may not see, so existence is not leaked.
    public static readonly Error NotFound = new(
        "notebook_not_found",
        "The notebook was not found.",
        ErrorKind.NotFound
    );

    public static readonly Error SlugAlreadyTaken = new(
        "slug_already_taken",
        "A notebook with this slug already exists.",
        ErrorKind.Conflict
    );

    // Distinct from NotFound so the client knows to prompt for the code; Unlisted notebooks
    // are discoverable by link anyway, so this leaks nothing.
    public static readonly Error AccessCodeRequired = new(
        "access_code_required",
        "This notebook requires an access code.",
        ErrorKind.Forbidden
    );

    public static readonly Error ShareTargetNotFound = new(
        "share_target_not_found",
        "No registered user has this email address.",
        ErrorKind.NotFound
    );

    public static readonly Error CannotShareWithOwner = new(
        "cannot_share_with_owner",
        "The owner already has full access.",
        ErrorKind.Validation
    );
}
