using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.PersonalAccessTokens.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;

namespace CodeCafe.Application.Auth.PersonalAccessTokens.ListPersonalAccessTokens;

public sealed class ListPersonalAccessTokensQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IPersonalAccessTokenRepository tokens
) : IQueryHandler<ListPersonalAccessTokensQuery, Result<IReadOnlyList<PersonalAccessTokenDto>>>
{
    public async Task<Result<IReadOnlyList<PersonalAccessTokenDto>>> Handle(
        ListPersonalAccessTokensQuery query,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserAccessor.User?.Id;
        if (userId is null)
        {
            return Result.Failure<IReadOnlyList<PersonalAccessTokenDto>>(AuthErrors.UserNotFound);
        }

        var own = await tokens.ListByUserAsync(userId.Value, cancellationToken);

        return Result.Success<IReadOnlyList<PersonalAccessTokenDto>>(
            own.Select(PersonalAccessTokenDto.From).ToList()
        );
    }
}
