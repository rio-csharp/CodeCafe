using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IQuery<AuthResult<AuthUserModel>>;
