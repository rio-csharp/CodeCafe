using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;

namespace CodeCafe.UnitTests.Application.Notes;

public sealed class NotesSettingsServiceTests
{
    [Fact]
    public async Task GetAsync_maps_domain_settings_to_response()
    {
        var repository = new StubNotesSettingsRepository(new NotesSettings("  /srv/notes  "));
        var service = new NotesSettingsService(repository);

        var response = await service.GetAsync(CancellationToken.None);

        Assert.Equal("/srv/notes", response.RootPath);
    }

    [Fact]
    public async Task UpdateAsync_updates_and_persists_trimmed_root_path()
    {
        var repository = new StubNotesSettingsRepository(new NotesSettings("/srv/notes"));
        var service = new NotesSettingsService(repository);

        var response = await service.UpdateAsync("  /srv/new-notes  ", CancellationToken.None);

        Assert.Equal("/srv/new-notes", response.RootPath);
        Assert.NotNull(repository.SavedSettings);
        Assert.Equal("/srv/new-notes", repository.SavedSettings!.RootPath);
    }

    private sealed class StubNotesSettingsRepository(NotesSettings settings) : INotesSettingsRepository
    {
        public NotesSettings? SavedSettings { get; private set; }

        public Task<NotesSettings> GetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(settings);
        }

        public Task SaveAsync(NotesSettings savedSettings, CancellationToken cancellationToken)
        {
            SavedSettings = savedSettings;
            return Task.CompletedTask;
        }
    }
}
