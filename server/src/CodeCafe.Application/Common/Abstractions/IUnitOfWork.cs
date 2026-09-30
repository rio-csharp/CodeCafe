namespace CodeCafe.Application.Common.Abstractions;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);

    // Structural block mutations must span lock -> mutate -> save in one explicit transaction:
    // LockPageAsync's FOR UPDATE is released at statement end in autocommit, so without this the
    // lock would be gone before the mutation runs.
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
