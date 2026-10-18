namespace CodeCafe.Application.Common.Abstractions;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);

    // Structural block mutations must span lock -> mutate -> save in one explicit transaction:
    // LockPageAsync's FOR UPDATE is released at statement end in autocommit, so without this the
    // lock would be gone before the mutation runs.
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    // Drops every tracked change without touching the database. A handler that mutates tracked
    // entities and then returns without saving (dry run, or a failed all-or-nothing batch) must
    // call this: the scoped DbContext can outlive the handler in a multi-tool AI request, and a
    // later SaveChanges would otherwise flush those abandoned mutations. The default is a no-op
    // for test doubles; AppDbContext overrides it with ChangeTracker.Clear().
    void DiscardChanges() { }
}
