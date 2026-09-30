namespace CodeCafe.Application.Common.Abstractions;

// An explicit database transaction. Disposing without committing rolls back, so a handler that
// bails out mid-way never leaves a partial structural change behind.
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
