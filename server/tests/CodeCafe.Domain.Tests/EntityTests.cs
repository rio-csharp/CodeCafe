using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Domain.Tests;

public sealed class EntityTests
{
    [Fact]
    public void Same_Id_And_Type_Are_Equal()
    {
        var id = Guid.NewGuid();
        var first = CreateItem(id);
        var second = CreateItem(id);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Different_Id_Are_Not_Equal()
    {
        Assert.NotEqual(CreateItem(Guid.NewGuid()), CreateItem(Guid.NewGuid()));
    }

    [Fact]
    public void Same_Id_But_Different_Type_Are_Not_Equal()
    {
        var id = Guid.NewGuid();
        var item = CreateItem(id);
        var notebook = Notebook.Create(
            id,
            Guid.NewGuid(),
            "Notes",
            NotebookSlug.Create("notes"),
            null,
            NotebookVisibility.Private,
            DateTimeOffset.UtcNow
        );

        Assert.NotEqual<Domain.Common.Entity>(item, notebook);
    }

    [Fact]
    public void Transient_Entities_Are_Not_Equal()
    {
        var first = CreateItem(Guid.NewGuid());
        var second = CreateItem(Guid.NewGuid());
        first.ClearDomainEvents();
        second.ClearDomainEvents();

        Assert.NotEqual(first, second);
    }

    private static NotebookItem CreateItem(Guid id)
    {
        return NotebookItem.Create(
            id,
            Guid.NewGuid(),
            null,
            NotebookItemType.Page,
            "Page",
            NotebookSlug.Create("page"),
            NotebookPath.Create("page"),
            0,
            DateTimeOffset.UtcNow
        );
    }
}
