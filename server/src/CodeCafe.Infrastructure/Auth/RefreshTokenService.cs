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
        var stored = await FindByTokenAsync(token, cancellationToken);
        if (stored is null || !stored.IsActive(DateTimeOffset.UtcNow))
        {
            return null;
        }

        stored.Revoke(DateTimeOffset.UtcNow);
        return stored.UserId;
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
    private async Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        byte[] bytes = new byte[TokenByteCount];
        var status = Base64Url.DecodeFromChars(token.AsSpan(), bytes, out _, out var written);
        if (status != System.Buffers.OperationStatus.Done || written != TokenByteCount)
        {
            return null;
        }

        return await dbContext.RefreshTokens.FirstOrDefaultAsync(
            candidate => candidate.TokenHash == HashToken(bytes),
            cancellationToken
        );
    }

    private static string HashToken(byte[] tokenBytes) => Convert.ToHexString(SHA256.HashData(tokenBytes));
}
