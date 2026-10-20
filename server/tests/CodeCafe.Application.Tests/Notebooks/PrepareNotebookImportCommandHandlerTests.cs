using CodeCafe.Application.Auth;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.PrepareNotebookImport;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class PrepareNotebookImportCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsANotebookGrantForTheCurrentUser()
    {
        var user = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        var handler = new PrepareNotebookImportCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(user.Id)),
            new StubUserRepository { user }
        );

        var result = await handler.Handle(
            new PrepareNotebookImportCommand(@"C:\notes\interview-prep.md", 8192),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var grant = result.Value!;
        Assert.Equal(ImportUploadKind.Notebook, grant.Kind);
        Assert.Equal(user.Id, grant.UserId);
        Assert.Null(grant.NotebookId);
        Assert.Null(grant.ParentPath);
        Assert.Equal("interview-prep.md", grant.FileName);
        Assert.Equal(8192, grant.MaxBytes);
    }

    [Fact]
    public async Task Handle_RejectsADeletedUser()
    {
        var handler = new PrepareNotebookImportCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(Guid.CreateVersion7())),
            new StubUserRepository()
        );

        var result = await handler.Handle(
            new PrepareNotebookImportCommand("interview-prep.md", 8192),
            CancellationToken.None
        );

        Assert.Equal(AuthErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_RejectsANonPositiveLimit()
    {
        var user = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        var handler = new PrepareNotebookImportCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(user.Id)),
            new StubUserRepository { user }
        );

        var result = await handler.Handle(
            new PrepareNotebookImportCommand("interview-prep.md", 0),
            CancellationToken.None
        );

        Assert.Equal(ImportUploadErrors.InvalidMaxBytes, result.Error);
    }
}
