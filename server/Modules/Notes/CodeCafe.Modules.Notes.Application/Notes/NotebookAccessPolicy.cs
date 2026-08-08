using CodeCafe.Domain.Notes;

namespace CodeCafe.Modules.Notes.Application.Notes;

public static class NotebookAccessPolicy
{
    public static bool CanReadNotebook(Notebook notebook, Guid currentUserId)
        => CanReadNotebook(notebook.OwnerId, notebook.Visibility, notebook.IsPublished, currentUserId);

    public static bool CanReadNotebook(
        Guid ownerId,
        NotebookVisibility visibility,
        bool isPublished,
        Guid currentUserId)
    {
        if (ownerId == currentUserId)
        {
            return true;
        }

        if (visibility == NotebookVisibility.Unlisted)
        {
            return true;
        }

        return visibility == NotebookVisibility.Public && isPublished;
    }
}
