using CodeCafe.Application.Common;

namespace CodeCafe.Application.Revisions.Shared;

public static class RevisionErrors
{
    // Also used for revisions of pages the caller may not see, so existence is not leaked.
    public static readonly Error NotFound = new(
        "revision_not_found",
        "The revision was not found.",
        ErrorKind.NotFound
    );

    // Restoring would re-create a block id that is still alive on the page.
    public static readonly Error RestoreConflict = new(
        "revision_restore_conflict",
        "The restore conflicts with blocks that currently exist on the page.",
        ErrorKind.Conflict
    );
}
