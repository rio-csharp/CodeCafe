using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Persistence;

public sealed class PersonalAccessTokenRepository(AppDbContext dbContext) : IPersonalAccessTokenRepository
{
    public async Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken)
        => await dbContext.PersonalAccessTokens.AddAsync(token, cancellationToken);

    public Task<PersonalAccessToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken)
        => dbContext.PersonalAccessTokens.FirstOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken
        );

    public async Task<IReadOnlyList<PersonalAccessToken>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext
            .PersonalAccessTokens.Where(token => token.UserId == userId)
            .OrderByDescending(token => token.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
