using CodeCafe.Application.Auth;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.CreateNotebook;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests;

public sealed class CreateNotebookCommandHandlerTests
{
    private static readonly CreateNotebookCommand Command =
        new("  My First Notebook!  ", null, null, NotebookVisibility.Private);

    [Fact]
    public async Task Handle_CreatesNotebook_WithGeneratedSlug()
    {
        var owner = SeedOwner();
        var notebooks = new StubNotebookRepository();
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(owner, notebooks, unitOfWork);

        var result = await handler.Handle(Command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var notebook = Assert.Single(notebooks);
        Assert.Equal(owner.Id, notebook.OwnerId);
        Assert.Equal("My First Notebook!", notebook.Title);
        Assert.Null(notebook.Description);
        Assert.Equal("my-first-notebook", notebook.Slug);
        Assert.Equal(NotebookVisibility.Private, notebook.Visibility);
        Assert.Null(notebook.DeletedAtUtc);

        var dto = result.Value!;
        Assert.Equal(notebook.Id, dto.Id);
        Assert.Equal(notebook.Slug, dto.Slug);
        Assert.False(dto.HasAccessCode);
        Assert.Empty(dto.Tags);
        Assert.Empty(dto.Shares);
        Assert.Equal(0, dto.PageCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_GeneratesUnicodeSlug_FromCjkTitle()
    {
        var owner = SeedOwner();
        var notebooks = new StubNotebookRepository();
        var handler = CreateHandler(owner, notebooks, new StubUnitOfWork());

        var result = await handler.Handle(
            Command with { Title = "我的笔记" },
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("我的笔记", Assert.Single(notebooks).Slug);
    }

    [Fact]
    public async Task Handle_UsesNormalizedExplicitSlug()
    {
        var owner = SeedOwner();
        var notebooks = new StubNotebookRepository();
        var handler = CreateHandler(owner, notebooks, new StubUnitOfWork());

        var result = await handler.Handle(
            Command with { Slug = " Custom-Slug " },
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("custom-slug", Assert.Single(notebooks).Slug);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenExplicitSlugIsTaken()
    {
        var owner = SeedOwner();
        var notebooks = new StubNotebookRepository
        {
            Notebook.Create(owner.Id, "Existing", null, "custom-slug", NotebookVisibility.Private)
        };
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(owner, notebooks, unitOfWork);

        var result = await handler.Handle(
            Command with { Slug = "custom-slug" },
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.SlugAlreadyTaken, result.Error);
        Assert.Single(notebooks);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenASlugIsTakenByAConcurrentCreate()
    {
        var owner = SeedOwner();
        var notebooks = new StubNotebookRepository();
        // The pre-check sees no conflict; the database rejects the write, exactly as it would for
        // a create that raced with another one.
        var unitOfWork = new StubUnitOfWork(new UniqueConstraintViolationException("IX_notebooks_Slug"));
        var handler = CreateHandler(owner, notebooks, unitOfWork);

        var result = await handler.Handle(
            Command with { Slug = "custom-slug" },
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.SlugAlreadyTaken, result.Error);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_RetriesWithAFreshCandidate_WhenAGeneratedSlugLosesARace()
    {
        var owner = SeedOwner();
        var notebooks = new StubNotebookRepository();
        // The first save loses the race; the retry must succeed without surfacing an error.
        var unitOfWork = new StubUnitOfWork(new UniqueConstraintViolationException("IX_notebooks_Slug"))
        {
            FailuresRemaining = 1
        };
        var handler = CreateHandler(owner, notebooks, unitOfWork);

        var result = await handler.Handle(Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
        // The stub repository already holds the staged notebook, so the retry sees the base
        // slug as taken and picks a suffixed variant on the same instance.
        var notebook = Assert.Single(notebooks);
        Assert.Matches(@"^my-first-notebook-\d{4}$", notebook.Slug);
    }

    [Fact]
    public async Task Handle_UsesASuffixedVariant_WhenGeneratedSlugIsTaken()
    {
        var owner = SeedOwner();
        var notebooks = new StubNotebookRepository
        {
            Notebook.Create(owner.Id, "My First Notebook", null, "my-first-notebook", NotebookVisibility.Private)
        };
        var handler = CreateHandler(owner, notebooks, new StubUnitOfWork());

        var result = await handler.Handle(Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Matches(@"^my-first-notebook-\d{4}$", result.Value!.Slug);
        Assert.Equal(2, notebooks.Count);
    }

    [Fact]
    public async Task Handle_KeepsSlugWithinTheStoredColumnLength_WhenTheGeneratedSlugIsTaken()
    {
        var owner = SeedOwner();
        var title = new string('a', Notebook.MaxTitleLength);
        var generated = NotebookSlug.GenerateFromTitle(title);
        var notebooks = new StubNotebookRepository
        {
            Notebook.Create(owner.Id, "Existing", null, generated, NotebookVisibility.Private)
        };
        var handler = CreateHandler(owner, notebooks, new StubUnitOfWork());

        var result = await handler.Handle(Command with { Title = title }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(generated, result.Value!.Slug);
        Assert.True(result.Value.Slug.Length <= Notebook.MaxSlugLength);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenNoCurrentUser()
    {
        var unitOfWork = new StubUnitOfWork();
        var handler = new CreateNotebookCommandHandler(
            new StubCurrentUserAccessor(null),
            new StubUserRepository(),
            new StubNotebookRepository(),
            unitOfWork
        );

        var result = await handler.Handle(Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static CreateNotebookCommandHandler CreateHandler(
        User owner,
        StubNotebookRepository notebooks,
        StubUnitOfWork unitOfWork
    )
        => new(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            new StubUserRepository { owner },
            notebooks,
            unitOfWork
        );
}
