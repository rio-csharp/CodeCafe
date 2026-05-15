namespace CodeCafe.Application.Notes;

public interface INotesService
{
    Task<IReadOnlyCollection<NoteSummary>> ListAsync(CancellationToken cancellationToken);

    Task<NoteContent?> ReadAsync(string path, CancellationToken cancellationToken);
}
