using CodeCafe.Domain.Notes;

namespace CodeCafe.Infrastructure.Notes;


public sealed class InMemoryNotesSettingsRepository : INotesSettingsRepository
{
    private readonly Lock syncRoot = new();
    private NotesSettings settings;

    public InMemoryNotesSettingsRepository(string initialRootPath = "")
    {
        settings = new NotesSettings(initialRootPath);
    }

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
