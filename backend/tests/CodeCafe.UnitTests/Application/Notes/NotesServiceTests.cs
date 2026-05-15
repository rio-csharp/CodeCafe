using CodeCafe.Application.Notes;

namespace CodeCafe.UnitTests.Application.Notes;

public sealed class NotesServiceTests
{
    [Fact]
    public async Task ListAsync_returns_repository_results()
    {
        var expected = new[]
        {
            new NoteSummary("a.md", "A", DateTimeOffset.UtcNow, 128),
        };
        var repository = new StubNotesRepository
        {
            Notes = expected,
        };
        var service = new NotesService(repository);

        var notes = await service.ListAsync(CancellationToken.None);

        Assert.Same(expected, notes);
    }

    [Fact]
    public async Task ReadAsync_returns_repository_result_for_requested_path()
    {
        var expected = new NoteContent("a.md", "A", DateTimeOffset.UtcNow, 128, "# A");
        var repository = new StubNotesRepository
        {
            Note = expected,
        };
        var service = new NotesService(repository);

        var note = await service.ReadAsync("a.md", CancellationToken.None);

        Assert.Same(expected, note);
        Assert.Equal("a.md", repository.LastRequestedPath);
    }

    private sealed class StubNotesRepository : INotesRepository
    {
        public IReadOnlyCollection<NoteSummary> Notes { get; init; } = [];

        public NoteContent? Note { get; init; }

        public string? LastRequestedPath { get; private set; }

        public Task<IReadOnlyCollection<NoteSummary>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Notes);
        }

        public Task<NoteContent?> ReadAsync(string path, CancellationToken cancellationToken)
        {
            LastRequestedPath = path;
            return Task.FromResult(Note);
        }
    }
}
