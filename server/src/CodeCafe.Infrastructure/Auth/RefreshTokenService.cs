using System.Buffers.Text;
using System.Security.Cryptography;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Domain.Identity;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeCafe.Infrastructure.Auth;

public sealed class RefreshTokenService(AppDbContext dbContext, IOptions<AuthOptions> options)
    : IRefreshTokenService
{
    private const int TokenByteCount = 64;

    public Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteCount);
        var expiresAtUtc = DateTimeOffset.UtcNow.AddDays(options.Value.RefreshTokenLifetimeDays);

        // The database stores only the hash, so a leak of the table does not leak live tokens.
        // Staged only: the caller's unit of work commits this together with the rest of the use case.
        dbContext.RefreshTokens.Add(
            RefreshToken.Create(userId, HashToken(bytes), expiresAtUtc)
        );

        return Task.FromResult(new IssuedRefreshToken(Base64Url.EncodeToString(bytes), expiresAtUtc));
    }

    public async Task<Guid?> ConsumeAsync(string token, CancellationToken cancellationToken)
    {
        var hash = TryComputeHash(token);
        if (hash is null)
        {
            return null;
        }

        var userId = await dbContext
            .RefreshTokens.Where(candidate => candidate.TokenHash == hash)
            .Select(candidate => (Guid?)candidate.UserId)
            .SingleOrDefaultAsync(cancellationToken);
        if (userId is null)
        {
            return null;
        }

        // The conditional UPDATE is the race judge: rotation depends on single use, and a
        // read-then-revoke in application code always leaves a window for concurrent requests.
        // Exactly one consumer rewrites an active row; the rest affect 0 rows and lose. Runs
        // inside the caller's transaction, so the winner's row lock is held until commit and
        // losers re-evaluate the predicate after waiting on the lock.
        var nowUtc = DateTimeOffset.UtcNow;
        var consumed = await dbContext
            .RefreshTokens.Where(candidate =>
                candidate.TokenHash == hash
                && candidate.RevokedAtUtc == null
                && candidate.ExpiresAtUtc > nowUtc
            )
            .ExecuteUpdateAsync(
                update => update.SetProperty(candidate => candidate.RevokedAtUtc, nowUtc),
                cancellationToken
            );
        return consumed == 1 ? userId : null;
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken)
    {
        var stored = await FindByTokenAsync(token, cancellationToken);
        if (stored is not null && stored.IsActive(DateTimeOffset.UtcNow))
        {
            stored.Revoke(DateTimeOffset.UtcNow);
        }
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var activeTokens = await dbContext
            .RefreshTokens.Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(nowUtc);
        }
    }

    // Tokens are always TokenByteCount bytes; decoding into a fixed buffer also rejects
    // malformed input by length alone.
    private static string? TryComputeHash(string token)
    {
        byte[] bytes = new byte[TokenByteCount];
        var status = Base64Url.DecodeFromChars(token.AsSpan(), bytes, out _, out var written);
        if (status != System.Buffers.OperationStatus.Done || written != TokenByteCount)
        {
            return null;
        }

        return HashToken(bytes);
    }

    private async Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        var hash = TryComputeHash(token);
        return hash is null
            ? null
            : await dbContext.RefreshTokens.FirstOrDefaultAsync(
                candidate => candidate.TokenHash == hash,
                cancellationToken
            );
    }

    private static string HashToken(byte[] tokenBytes) => Convert.ToHexString(SHA256.HashData(tokenBytes));
}
