using CodeCafe.Domain.Notes.Enums;

namespace CodeCafe.Application.Notes;

// Bundled as a record so future access dimensions (e.g. page-level shares) extend
// this type instead of churning the policy's parameter list at every call site.
public sealed record NotebookAccessContext(
    Guid OwnerId,
    NotebookVisibility Visibility,
    bool HasAccessCode,
    bool IsSharedWithUser,
    bool AccessCodeVerified
);

public static class NotebookAccessPolicy
{
    public static bool CanReadNotebook(NotebookAccessContext context, Guid currentUserId)
    {
        if (context.OwnerId == currentUserId || context.IsSharedWithUser)
        {
            return true;
        }

        if (context.Visibility == NotebookVisibility.Unlisted)
        {
            return !context.HasAccessCode || context.AccessCodeVerified;
        }

        return context.Visibility == NotebookVisibility.Public;
    }

    public static bool RequiresAccessCode(NotebookAccessContext context, Guid currentUserId)
    {
        return context.Visibility == NotebookVisibility.Unlisted
            && context.HasAccessCode
            && !context.AccessCodeVerified
            && context.OwnerId != currentUserId
            && !context.IsSharedWithUser;
    }
}
