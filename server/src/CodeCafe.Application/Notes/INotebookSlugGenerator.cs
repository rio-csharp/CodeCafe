using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Application.Notes;

public interface INotebookSlugGenerator
{
    Task<NotebookSlug> GenerateUniqueSlugAsync(
        string title,
        Guid? excludeNotebookId,
        CancellationToken cancellationToken
    );

    Task<bool> IsSlugAvailableAsync(
        NotebookSlug slug,
        Guid? excludeNotebookId,
        CancellationToken cancellationToken
    );
}
