using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Domain.Tests;

public sealed class PageChainTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static Notebook NewNotebook() => Notebook.Create(OwnerId, "Notebook", null, "notebook", NotebookVisibility.Private);

    private static Page NewPage(Notebook notebook, string slug, Page? parent = null)
        => Page.Create(notebook.Id, parent?.Id, slug, slug, slug);

    [Fact]
    public void Link_AfterSibling_PointsPredecessorForward()
    {
        var notebook = NewNotebook();
        var a = NewPage(notebook, "a");
        var b = NewPage(notebook, "b");

        PageChain.Link(b, notebook, parent: null, after: a);

        Assert.Equal(b.Id, a.NextSiblingId);
        Assert.Null(notebook.FirstPageId);
    }

    [Fact]
    public void Link_AtRootHead_UpdatesNotebookFirstPage()
    {
        var notebook = NewNotebook();
        var a = NewPage(notebook, "a");

        PageChain.Link(a, notebook, parent: null, after: null);

        Assert.Equal(a.Id, notebook.FirstPageId);
    }

    [Fact]
    public void Link_AtChildChainHead_UpdatesParentFirstChild()
    {
        var notebook = NewNotebook();
        var parent = NewPage(notebook, "p");
        var child = NewPage(notebook, "c", parent);

        PageChain.Link(child, notebook, parent, after: null);

        Assert.Equal(child.Id, parent.FirstChildId);
    }

    [Fact]
    public void Unlink_MiddleSibling_PredecessorSkipsOverIt()
    {
        var notebook = NewNotebook();
        var a = NewPage(notebook, "a");
        var b = NewPage(notebook, "b");
        var c = NewPage(notebook, "c");
        a.SetNextSibling(b.Id);
        b.SetNextSibling(c.Id);

        PageChain.Unlink(b, notebook, ancestors: [], oldChain: [a, b, c]);

        Assert.Equal(c.Id, a.NextSiblingId);
    }

    [Fact]
    public void Unlink_RootHead_NotebookHeadPassesToNextSibling()
    {
        var notebook = NewNotebook();
        var a = NewPage(notebook, "a");
        var b = NewPage(notebook, "b");
        a.SetNextSibling(b.Id);
        notebook.SetFirstPage(a.Id);

        PageChain.Unlink(a, notebook, ancestors: [], oldChain: [a, b]);

        Assert.Equal(b.Id, notebook.FirstPageId);
    }

    [Fact]
    public void Unlink_ChildChainHead_ParentHeadPassesToNextSibling()
    {
        var notebook = NewNotebook();
        var parent = NewPage(notebook, "p");
        var a = NewPage(notebook, "a", parent);
        var b = NewPage(notebook, "b", parent);
        a.SetNextSibling(b.Id);
        parent.SetFirstChild(a.Id);

        PageChain.Unlink(a, notebook, ancestors: [parent], oldChain: [a, b]);

        Assert.Equal(b.Id, parent.FirstChildId);
    }

    [Fact]
    public void Move_ToAnotherParent_FixesBothChains()
    {
        var notebook = NewNotebook();
        var a = NewPage(notebook, "a");
        var b = NewPage(notebook, "b");
        a.SetNextSibling(b.Id);
        var p = NewPage(notebook, "p");
        var c = NewPage(notebook, "c", p);

        // Move b under p, after c.
        PageChain.Move(b, notebook, p, ancestors: [], oldSiblings: [a, b], newSiblings: [c], insertIndex: 1);

        Assert.Equal(p.Id, b.ParentId);
        Assert.Null(a.NextSiblingId);
        Assert.Equal(b.Id, c.NextSiblingId);
        Assert.Null(b.NextSiblingId);
        Assert.True(string.CompareOrdinal(c.SortKey, b.SortKey) < 0);
    }

    [Fact]
    public void Move_WithinSameParent_Reorders()
    {
        var notebook = NewNotebook();
        var a = NewPage(notebook, "a");
        var b = NewPage(notebook, "b");
        var c = NewPage(notebook, "c");
        a.SetNextSibling(b.Id);
        b.SetNextSibling(c.Id);

        // Move c between a and b.
        PageChain.Move(c, notebook, newParent: null, ancestors: [], oldSiblings: [a, b, c], newSiblings: [a, b], insertIndex: 1);

        Assert.Null(b.NextSiblingId);
        Assert.Equal(c.Id, a.NextSiblingId);
        Assert.Equal(b.Id, c.NextSiblingId);
        Assert.True(string.CompareOrdinal(a.SortKey, c.SortKey) < 0);
        Assert.True(string.CompareOrdinal(c.SortKey, b.SortKey) < 0);
    }

    [Fact]
    public void IsSelfOrDescendant_DetectsSelfAndDescendants()
    {
        var notebook = NewNotebook();
        var page = NewPage(notebook, "page");
        var child = NewPage(notebook, "child", page);
        var unrelated = NewPage(notebook, "unrelated");

        Assert.True(PageChain.IsSelfOrDescendant(page, page, []));
        Assert.True(PageChain.IsSelfOrDescendant(page, child, [page]));
        Assert.False(PageChain.IsSelfOrDescendant(page, unrelated, []));
    }
}
