using CodeCafe.Application.Auth.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<Result<AuthSessionDto>>;
