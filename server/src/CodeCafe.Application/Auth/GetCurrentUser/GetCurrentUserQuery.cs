using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.GetCurrentUser;

public sealed record GetCurrentUserQuery() : IQuery<Result<AuthUserDto>>;
