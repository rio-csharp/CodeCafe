namespace CodeCafe.Contracts.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginSessionResponse(bool IsAuthenticated, string? Username);
