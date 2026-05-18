namespace CodeCafe.WebApi.Auth;

public sealed record AuthResponse(UserResponse User);

public sealed record LogoutResponse(bool Success);

public sealed record UserResponse(Guid Id, string Email, string DisplayName);
