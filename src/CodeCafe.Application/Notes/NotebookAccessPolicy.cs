using CodeCafe.Domain.Notes;

namespace CodeCafe.Application.Notes;

public static class NotebookAccessPolicy
{
    public static bool CanReadNotebook(Notebook notebook, Guid currentUserId)
    {
        if (notebook.OwnerId == currentUserId)
        {
            return true;
        }

        if (notebook.Visibility == NotebookVisibility.Unlisted)
        {
            return true;
        }

        return notebook.Visibility == NotebookVisibility.Public && notebook.IsPublished;
    }
}
