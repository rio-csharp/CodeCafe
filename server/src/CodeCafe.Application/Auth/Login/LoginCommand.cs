using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<Result<AuthSessionDto>>;
