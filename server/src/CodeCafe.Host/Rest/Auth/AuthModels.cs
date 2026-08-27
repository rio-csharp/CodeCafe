using System.ComponentModel.DataAnnotations;

namespace CodeCafe.Host.Rest.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, StringLength(40, MinimumLength = 1)] string DisplayName
);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public sealed record AuthResponse(UserResponse User);

public sealed record LogoutResponse(bool Success);

public sealed record UserResponse(Guid Id, string Email, string DisplayName);
