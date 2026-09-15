namespace CodeCafe.Application.Auth.Models;

public sealed record AuthUserDto(Guid Id, string Email, string DisplayName);
