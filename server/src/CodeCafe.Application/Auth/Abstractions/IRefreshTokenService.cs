namespace CodeCafe.Application.Auth.Abstractions;

public interface IRefreshTokenService
{
    RefreshToken Issue(Guid userId);

    Guid? Validate(string token);
}

public sealed record RefreshToken(string Value, DateTimeOffset ExpiresAtUtc);
