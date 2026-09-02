using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Commands.ChangeNotebookSlug;
using CodeCafe.Infrastructure.Notes.Read;
using CodeCafe.Infrastructure.Notes.Write;
using CodeCafe.Infrastructure.Persistence;

namespace CodeCafe.Infrastructure.Tests;

[Collection("postgres")]
public sealed class ChangeNotebookSlugCommandHandlerTests : IDisposable
{
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly NotesDbHarness _harness;

    public ChangeNotebookSlugCommandHandlerTests(PostgresFixture fixture) => _harness = new NotesDbHarness(fixture);

    [Fact]
    public async Task Owner_Changes_Slug()
    {
        await using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var notebook = _harness.SeedNotebook(context, OwnerId, "My Notes", "my-notes-1");

        var result = await CreateHandler(context).Handle(
            new ChangeNotebookSlugCommand(OwnerId, notebook.Id, "renamed-notes"),
            CancellationToken.None
        );

        Assert.True(result.Succeeded);
        Assert.Equal("renamed-notes", result.Value!.Slug);
    }

    [Fact]
    public async Task Non_Owner_Is_Forbidden()
    {
        await using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        _harness.SeedUser(context, OtherId, "Bob");
        var notebook = _harness.SeedNotebook(context, OtherId, "Bob Notes", "bobs-notes");

        var result = await CreateHandler(context).Handle(
            new ChangeNotebookSlugCommand(OwnerId, notebook.Id, "stolen-slug"),
            CancellationToken.None
        );

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task Taken_Slug_Returns_Conflict()
    {
        await using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        _harness.SeedNotebook(context, OwnerId, "Existing", "taken-slug-1");
        var notebook = _harness.SeedNotebook(context, OwnerId, "My Notes", "my-notes-1");

        var result = await CreateHandler(context).Handle(
            new ChangeNotebookSlugCommand(OwnerId, notebook.Id, "taken-slug-1"),
            CancellationToken.None
        );

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.Conflict, result.Error!.Kind);
        Assert.Equal("slug_taken", result.Error.Code);
    }

    [Fact]
    public async Task Missing_Notebook_Returns_NotFound()
    {
        await using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");

        var result = await CreateHandler(context).Handle(
            new ChangeNotebookSlugCommand(OwnerId, Guid.NewGuid(), "some-slug-1"),
            CancellationToken.None
        );

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.NotFound, result.Error!.Kind);
    }

    private ChangeNotebookSlugCommandHandler CreateHandler(ApplicationDbContext context) =>
        new(
            new NotebookRepository(context),
            new NotebookSlugGenerator(context),
            new NotebookReadService(context, new FakeAccessCodeHasher())
        );

    public void Dispose() => _harness.Dispose();
}
