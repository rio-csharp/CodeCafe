namespace CodeCafe.Application.Notes;


public sealed class NotesSettingsService(INotesSettingsRepository repository) : INotesSettingsService
{
    public async Task<NotesSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await repository.GetAsync(cancellationToken);

        return new NotesSettingsDto(settings.RootPath);
    }

    public async Task<NotesSettingsDto> UpdateAsync(
        string rootPath,
        CancellationToken cancellationToken)
    {
        var settings = await repository.GetAsync(cancellationToken);

        settings.UpdateRootPath(rootPath);
        await repository.SaveAsync(settings, cancellationToken);

        return new NotesSettingsDto(settings.RootPath);
    }
}
