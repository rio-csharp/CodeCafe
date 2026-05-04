namespace CodeCafe.Application.Notes;

using CodeCafe.Domain.Notes;

public interface INotesSettingsRepository
{
    Task<NotesSettings> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(NotesSettings settings, CancellationToken cancellationToken);
}
