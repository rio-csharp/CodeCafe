namespace CodeCafe.Application.Notes;

using CodeCafe.Contracts.Notes;

public sealed class NotesSettingsService(INotesSettingsRepository repository) : INotesSettingsService
{
    public async Task<NotesSettingsResponse> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await repository.GetAsync(cancellationToken);

        return new NotesSettingsResponse(settings.RootPath);
    }

    public async Task<NotesSettingsResponse> UpdateAsync(
        UpsertNotesSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var settings = await repository.GetAsync(cancellationToken);

        settings.UpdateRootPath(request.RootPath);
        await repository.SaveAsync(settings, cancellationToken);

        return new NotesSettingsResponse(settings.RootPath);
    }
}
