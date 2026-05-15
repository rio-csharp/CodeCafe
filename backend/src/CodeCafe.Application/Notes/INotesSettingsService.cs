namespace CodeCafe.Application.Notes;

public interface INotesSettingsService
{
    Task<NotesSettingsDto> GetAsync(CancellationToken cancellationToken);

    Task<NotesSettingsDto> UpdateAsync(string rootPath, CancellationToken cancellationToken);
}
