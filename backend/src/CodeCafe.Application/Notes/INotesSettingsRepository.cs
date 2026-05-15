using CodeCafe.Domain.Notes;

namespace CodeCafe.Application.Notes;


public interface INotesSettingsRepository
{
    Task<NotesSettings> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(NotesSettings settings, CancellationToken cancellationToken);
}
