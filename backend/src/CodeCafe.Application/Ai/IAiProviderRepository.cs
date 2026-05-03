namespace CodeCafe.Application.Ai;

using CodeCafe.Domain.Ai;

public interface IAiProviderRepository
{
    Task<IReadOnlyCollection<AiProviderConfiguration>> ListAsync(CancellationToken cancellationToken);

    Task<AiProviderConfiguration?> GetAsync(Guid providerId, CancellationToken cancellationToken);

    Task AddAsync(AiProviderConfiguration provider, CancellationToken cancellationToken);

    Task DeleteAsync(Guid providerId, CancellationToken cancellationToken);
}
