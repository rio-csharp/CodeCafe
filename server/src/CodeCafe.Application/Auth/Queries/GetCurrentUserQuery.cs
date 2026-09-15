using CodeCafe.Application.Auth.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Queries;

public sealed record GetCurrentUserQuery() : IQuery<Result<AuthUserDto>>;
