using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Auth.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> FindByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
