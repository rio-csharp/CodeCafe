using CodeCafe.Application.Auth.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : ICommand<Result<AuthSessionDto>>;
