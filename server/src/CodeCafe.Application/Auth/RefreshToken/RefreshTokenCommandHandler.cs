using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IAccessTokenService accessTokens,
    IRefreshTokenService refreshTokens
) : ICommandHandler<RefreshTokenCommand, Result<AuthSessionDto>>
{
    public async Task<Result<AuthSessionDto>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        // ConsumeAsync wins its race with a conditional UPDATE; the row lock it takes must be
        // held until the replacement token is committed, hence the explicit transaction.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var userId = await refreshTokens.ConsumeAsync(command.RefreshToken, cancellationToken);
        var user = userId is { } id ? await users.FindByIdAsync(id, cancellationToken) : null;
        if (user is null)
        {
            return Result.Failure<AuthSessionDto>(AuthErrors.InvalidRefreshToken);
        }

        var accessToken = accessTokens.Issue(user.Id);
        var refreshToken = await refreshTokens.IssueAsync(user.Id, cancellationToken);

        // One commit: the old token's revocation and its replacement land together.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(
            new AuthSessionDto(
                new AuthUserDto(user.Id, user.Email, user.DisplayName),
                accessToken.Value,
                accessToken.ExpiresAtUtc,
                refreshToken.Value
            )
        );
    }
}
