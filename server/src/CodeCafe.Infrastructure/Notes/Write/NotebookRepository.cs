using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes.Write;

public sealed class NotebookRepository(ApplicationDbContext dbContext) : INotebookRepository
{
    public void Add(Notebook notebook) => dbContext.Notebooks.Add(notebook);

    public Task<Notebook?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Notebooks
            .Include(notebook => notebook.Items)
            .FirstOrDefaultAsync(notebook => notebook.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
