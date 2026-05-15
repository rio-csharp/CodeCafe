namespace CodeCafe.Application.Notes;

public interface INotesRepository
{
    Task<IReadOnlyCollection<NoteSummary>> ListAsync(CancellationToken cancellationToken);

    Task<NoteContent?> ReadAsync(string path, CancellationToken cancellationToken);
}
