using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Identity.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IQuery<AuthResult<AuthUserModel>>;
