using CodeCafe.Application.Auth.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Commands;

public sealed record UpdateProfileCommand(string DisplayName) : ICommand<Result<AuthUserDto>>;
