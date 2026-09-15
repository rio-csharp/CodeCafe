using CodeCafe.Application.Auth.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Commands;

public sealed record RegisterCommand(string Email, string Password, string DisplayName) : ICommand<Result<AuthSessionDto>>;
