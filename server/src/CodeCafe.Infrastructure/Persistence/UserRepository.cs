using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Persistence;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        => dbContext.Users.FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Users.FindAsync([id], cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
        => await dbContext.Users.AddAsync(user, cancellationToken);
}
