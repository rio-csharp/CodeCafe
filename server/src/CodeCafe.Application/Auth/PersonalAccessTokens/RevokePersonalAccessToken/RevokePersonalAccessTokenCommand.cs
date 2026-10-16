using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.PersonalAccessTokens.RevokePersonalAccessToken;

public sealed record RevokePersonalAccessTokenCommand(Guid Id) : ICommand<Result>;
