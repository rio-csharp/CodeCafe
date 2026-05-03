namespace CodeCafe.Application.Notes;

using CodeCafe.Contracts.Notes;

public interface INotesSettingsService
{
    Task<NotesSettingsResponse> GetAsync(CancellationToken cancellationToken);

    Task<NotesSettingsResponse> UpdateAsync(UpsertNotesSettingsRequest request, CancellationToken cancellationToken);
}
