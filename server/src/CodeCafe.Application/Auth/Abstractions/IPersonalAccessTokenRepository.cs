using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Auth.Abstractions;

public interface IPersonalAccessTokenRepository
{
    Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken);

    Task<PersonalAccessToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<PersonalAccessToken>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);
}
