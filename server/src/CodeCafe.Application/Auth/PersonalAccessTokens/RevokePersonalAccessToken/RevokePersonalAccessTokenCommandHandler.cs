using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;

namespace CodeCafe.Application.Auth.PersonalAccessTokens.RevokePersonalAccessToken;

public sealed class RevokePersonalAccessTokenCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IPersonalAccessTokenRepository tokens,
    IUnitOfWork unitOfWork
) : ICommandHandler<RevokePersonalAccessTokenCommand, Result>
{
    public async Task<Result> Handle(RevokePersonalAccessTokenCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        if (userId is null)
        {
            return Result.Failure(AuthErrors.UserNotFound);
        }

        // Looking up within the caller's own tokens makes a foreign token indistinguishable
        // from a nonexistent one, so token ids cannot be probed across users.
        var token = (await tokens.ListByUserAsync(userId.Value, cancellationToken))
            .FirstOrDefault(candidate => candidate.Id == command.Id);
        if (token is null)
        {
            return Result.Failure(AuthErrors.PersonalAccessTokenNotFound);
        }

        // Idempotent: revoking an already-revoked token is a no-op success.
        if (token.RevokedAtUtc is null)
        {
            token.Revoke(DateTimeOffset.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
