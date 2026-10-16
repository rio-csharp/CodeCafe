namespace CodeCafe.Application.Auth.Register;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName);
