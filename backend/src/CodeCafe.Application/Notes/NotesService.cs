namespace CodeCafe.Application.Notes;

using CodeCafe.Contracts.Notes;

public sealed class NotesService(INotesRepository repository) : INotesService
{
    public Task<IReadOnlyCollection<NoteSummaryResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return repository.ListAsync(cancellationToken);
    }

    public Task<NoteContentResponse?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        return repository.ReadAsync(path, cancellationToken);
    }
}
