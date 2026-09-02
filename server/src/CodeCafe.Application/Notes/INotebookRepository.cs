using CodeCafe.Domain.Notes;

namespace CodeCafe.Application.Notes;

public interface INotebookRepository
{
    void Add(Notebook notebook);

    Task<Notebook?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
