using CodeCafe.Application.Auth.PersonalAccessTokens.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.PersonalAccessTokens.ListPersonalAccessTokens;

public sealed record ListPersonalAccessTokensQuery() : IQuery<Result<IReadOnlyList<PersonalAccessTokenDto>>>;
