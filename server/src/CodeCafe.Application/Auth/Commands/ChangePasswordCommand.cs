using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Commands;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<Result>;
