namespace CodeCafe.Application.Notes;

public sealed class NotesService(INotesRepository repository) : INotesService
{
    public Task<IReadOnlyCollection<NoteSummary>> ListAsync(CancellationToken cancellationToken)
    {
        return repository.ListAsync(cancellationToken);
    }

    public Task<NoteContent?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        return repository.ReadAsync(path, cancellationToken);
    }
}
