using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.PersonalAccessTokens.CreatePersonalAccessToken;

public sealed record CreatePersonalAccessTokenCommand(string Name, int? ExpiresInDays)
    : ICommand<Result<CreatedPersonalAccessTokenDto>>;
