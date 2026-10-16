using System.Buffers.Text;
using System.Security.Cryptography;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.PersonalAccessTokens.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Auth.PersonalAccessTokens.CreatePersonalAccessToken;

public sealed class CreatePersonalAccessTokenCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IPersonalAccessTokenRepository tokens,
    IUnitOfWork unitOfWork
) : ICommandHandler<CreatePersonalAccessTokenCommand, Result<CreatedPersonalAccessTokenDto>>
{
    private const int TokenByteCount = 32;
    private const int DefaultLifetimeDays = 90;
    private const int MaxLifetimeDays = 365;

    public async Task<Result<CreatedPersonalAccessTokenDto>> Handle(
        CreatePersonalAccessTokenCommand command,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserAccessor.User?.Id;
        if (userId is null)
        {
            return Result.Failure<CreatedPersonalAccessTokenDto>(AuthErrors.UserNotFound);
        }

        // The validator enforces the same bounds; the clamp keeps direct handler calls safe.
        var lifetimeDays = Math.Clamp(command.ExpiresInDays ?? DefaultLifetimeDays, 1, MaxLifetimeDays);

        var tokenBytes = RandomNumberGenerator.GetBytes(TokenByteCount);
        var rawToken = PersonalAccessTokenHash.Prefix + Base64Url.EncodeToString(tokenBytes);

        var token = PersonalAccessToken.Create(
            userId.Value,
            command.Name.Trim(),
            PersonalAccessTokenHash.Compute(rawToken),
            DateTimeOffset.UtcNow.AddDays(lifetimeDays)
        );

        await tokens.AddAsync(token, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(
            new CreatedPersonalAccessTokenDto(
                token.Id,
                token.Name,
                token.CreatedAtUtc,
                token.ExpiresAtUtc,
                rawToken
            )
        );
    }
}
