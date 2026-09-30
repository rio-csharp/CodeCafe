using CodeCafe.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace CodeCafe.Infrastructure.Persistence;

internal sealed class EfTransaction(IDbContextTransaction inner) : ITransaction
{
    public Task CommitAsync(CancellationToken cancellationToken) => inner.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => inner.DisposeAsync();
}
