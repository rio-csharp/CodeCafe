using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand<Result>;
