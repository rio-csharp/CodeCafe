namespace CodeCafe.Application.Auth.Login;

public sealed record LoginRequest(
    string Email,
    string Password);
