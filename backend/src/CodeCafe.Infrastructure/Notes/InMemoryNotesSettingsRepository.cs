namespace CodeCafe.Infrastructure.Notes;

using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;

public sealed class InMemoryNotesSettingsRepository : INotesSettingsRepository
{
    private readonly Lock syncRoot = new();
    private NotesSettings settings = new(string.Empty);

    public Task<NotesSettings> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (syncRoot)
        {
            return Task.FromResult(settings);
        }
    }

    public Task SaveAsync(NotesSettings nextSettings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (syncRoot)
        {
            settings = nextSettings;
        }

        return Task.CompletedTask;
    }
}
