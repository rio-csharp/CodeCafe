using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Commands.CreateNotebook;
using CodeCafe.Infrastructure.Notes.Read;
using CodeCafe.Infrastructure.Notes.Write;
using CodeCafe.Infrastructure.Persistence;

namespace CodeCafe.Infrastructure.Tests;

[Collection("postgres")]
public sealed class CreateNotebookCommandHandlerTests : IDisposable
{
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly NotesDbHarness _harness;

    public CreateNotebookCommandHandlerTests(PostgresFixture fixture) => _harness = new NotesDbHarness(fixture);

    [Fact]
    public async Task Create_Public_Sets_PublishedAtUtc_And_Returns_Detail()
    {
        await using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");

        var result = await CreateHandler(context).Handle(
            new CreateNotebookCommand(OwnerId, "  My Notes  ", "  hello  ", "public"),
            CancellationToken.None
        );

        Assert.True(result.Succeeded);
        var detail = result.Value!;
        Assert.Equal("My Notes", detail.Title);
        Assert.Equal("my-notes", detail.Slug);
        Assert.Equal("hello", detail.Description);
        Assert.Equal(OwnerId, detail.OwnerId);
        Assert.NotNull(detail.PublishedAtUtc);
    }

    [Fact]
    public async Task Create_Private_Leaves_PublishedAtUtc_Null()
    {
        await using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");

        var result = await CreateHandler(context).Handle(
            new CreateNotebookCommand(OwnerId, "My Notes", null, null),
            CancellationToken.None
        );

        Assert.True(result.Succeeded);
        Assert.Null(result.Value!.PublishedAtUtc);
    }

    [Fact]
    public async Task Same_Title_Generates_Suffixed_Slug()
    {
        await using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var handler = CreateHandler(context);

        var first = await handler.Handle(
            new CreateNotebookCommand(OwnerId, "My Notes", null, null),
            CancellationToken.None
        );
        var second = await handler.Handle(
            new CreateNotebookCommand(OwnerId, "My Notes", null, null),
            CancellationToken.None
        );

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal("my-notes", first.Value!.Slug);
        Assert.Equal("my-notes-2", second.Value!.Slug);
    }

    [Fact]
    public async Task Undefined_Visibility_Returns_Validation_Failure()
    {
        await using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");

        var result = await CreateHandler(context).Handle(
            new CreateNotebookCommand(OwnerId, "My Notes", null, "bogus"),
            CancellationToken.None
        );

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.Validation, result.Error!.Kind);
        Assert.Equal("invalid_visibility", result.Error.Code);
    }

    [Fact]
    public async Task Explicit_Slug_Is_Used()
    {
        await using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");

        var result = await CreateHandler(context).Handle(
            new CreateNotebookCommand(OwnerId, "My Notes", null, null, "my-custom-slug"),
            CancellationToken.None
        );

        Assert.True(result.Succeeded);
        Assert.Equal("my-custom-slug", result.Value!.Slug);
    }

    [Fact]
    public async Task Taken_Explicit_Slug_Returns_Conflict()
    {
        await using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        _harness.SeedNotebook(context, OwnerId, "Existing", "my-custom-slug");

        var result = await CreateHandler(context).Handle(
            new CreateNotebookCommand(OwnerId, "My Notes", null, null, "my-custom-slug"),
            CancellationToken.None
        );

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.Conflict, result.Error!.Kind);
        Assert.Equal("slug_taken", result.Error.Code);
    }

    [Fact]
    public async Task Invalid_Explicit_Slug_Returns_Validation_Failure()
    {
        await using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");

        var result = await CreateHandler(context).Handle(
            new CreateNotebookCommand(OwnerId, "My Notes", null, null, "no way"),
            CancellationToken.None
        );

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.Validation, result.Error!.Kind);
        Assert.Equal("invalid_slug", result.Error.Code);
    }

    private CreateNotebookCommandHandler CreateHandler(ApplicationDbContext context) =>
        new(
            new NotebookRepository(context),
            new NotebookSlugGenerator(context),
            new NotebookReadService(context, new FakeAccessCodeHasher()),
            _harness.TimeProvider
        );

    public void Dispose() => _harness.Dispose();
}
