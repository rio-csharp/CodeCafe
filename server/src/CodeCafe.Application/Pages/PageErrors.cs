using CodeCafe.Application.Common;

namespace CodeCafe.Application.Pages;

public static class PageErrors
{
    // Also used for pages the caller may not see, so existence is not leaked.
    public static readonly Error NotFound = new(
        "page_not_found",
        "The page was not found.",
        ErrorKind.NotFound
    );

    public static readonly Error ParentNotFound = new(
        "parent_not_found",
        "The parent path does not match any page.",
        ErrorKind.NotFound
    );

    public static readonly Error SlugAlreadyTaken = new(
        "slug_already_taken",
        "A page with this slug already exists in the notebook.",
        ErrorKind.Conflict
    );

    public static readonly Error CannotMoveIntoDescendant = new(
        "cannot_move_into_descendant",
        "A page cannot be moved under itself or one of its descendants.",
        ErrorKind.Validation
    );

    public static readonly Error AfterPageNotFound = new(
        "after_page_not_found",
        "The page to insert after was not found under the new parent.",
        ErrorKind.NotFound
    );

    public static readonly Error ShareTargetNotFound = new(
        "share_target_not_found",
        "No registered user has this email address.",
        ErrorKind.NotFound
    );

    public static readonly Error CannotShareWithOwner = new(
        "cannot_share_with_owner",
        "The notebook owner already has full access.",
        ErrorKind.Validation
    );

    // A cursor the handler cannot decode is a caller bug: reject it instead of silently
    // restarting the feed from the top.
    public static readonly Error InvalidCursor = new(
        "invalid_cursor",
        "The cursor is malformed.",
        ErrorKind.Validation
    );

    public static readonly Error InvalidImport = new(
        "invalid_import",
        "The markdown file does not contain a page title.",
        ErrorKind.Validation
    );
}
