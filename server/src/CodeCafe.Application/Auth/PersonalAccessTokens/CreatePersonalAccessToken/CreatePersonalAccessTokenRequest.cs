namespace CodeCafe.Application.Auth.PersonalAccessTokens.CreatePersonalAccessToken;

public sealed record CreatePersonalAccessTokenRequest(string Name, int? ExpiresInDays);
