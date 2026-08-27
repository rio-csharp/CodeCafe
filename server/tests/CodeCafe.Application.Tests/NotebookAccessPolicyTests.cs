using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;

namespace CodeCafe.Application.Tests;

public sealed class NotebookAccessPolicyTests
{
    [Fact]
    public void Owner_CanRead_PrivateNotebook()
    {
        var ownerId = Guid.NewGuid();
        var notebook = CreateNotebook(ownerId, NotebookVisibility.Private, isPublished: false);

        Assert.True(NotebookAccessPolicy.CanReadNotebook(notebook, ownerId));
    }

    [Fact]
    public void NonOwner_CanRead_UnlistedNotebook()
    {
        var notebook = CreateNotebook(
            Guid.NewGuid(),
            NotebookVisibility.Unlisted,
            isPublished: false
        );

        Assert.True(NotebookAccessPolicy.CanReadNotebook(notebook, Guid.NewGuid()));
    }

    [Fact]
    public void NonOwner_CannotRead_UnpublishedPublicNotebook()
    {
        var notebook = CreateNotebook(
            Guid.NewGuid(),
            NotebookVisibility.Public,
            isPublished: false
        );

        Assert.False(NotebookAccessPolicy.CanReadNotebook(notebook, Guid.NewGuid()));
    }

    private static Notebook CreateNotebook(
        Guid ownerId,
        NotebookVisibility visibility,
        bool isPublished
    )
    {
        return new Notebook
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = "Notebook",
            Slug = "notebook",
            Visibility = visibility,
            IsPublished = isPublished,
        };
    }
}
