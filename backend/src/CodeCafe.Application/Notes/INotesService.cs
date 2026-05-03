namespace CodeCafe.Application.Notes;

using CodeCafe.Contracts.Notes;

public interface INotesService
{
    Task<IReadOnlyCollection<NoteSummaryResponse>> ListAsync(CancellationToken cancellationToken);

    Task<NoteContentResponse?> ReadAsync(string path, CancellationToken cancellationToken);
}
