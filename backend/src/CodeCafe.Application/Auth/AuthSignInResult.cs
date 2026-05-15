namespace CodeCafe.Application.Auth;

public sealed record AuthSignInResult(string Username, IssuedAuthToken Token);
