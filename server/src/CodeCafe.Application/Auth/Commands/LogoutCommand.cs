using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Commands;

public sealed record LogoutCommand() : ICommand<Result>;
