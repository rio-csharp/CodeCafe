using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Register;

public sealed record RegisterCommand(string Email, string Password, string DisplayName) : ICommand<Result<AuthSessionDto>>;
