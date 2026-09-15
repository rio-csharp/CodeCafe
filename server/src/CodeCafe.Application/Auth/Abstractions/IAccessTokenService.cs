namespace CodeCafe.Application.Auth.Abstractions;

public interface IAccessTokenService
{
    AccessToken Issue(Guid userId);

    Guid? Validate(string token);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);
