using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Domain.Pages;

// The sibling chains are a cross-entity structure: the head lives on the parent (FirstChildId)
// or the notebook (FirstPageId), and each link is a neighbour's NextSiblingId. These operations
// keep that structure consistent, so handlers never fix up chain pointers themselves.
public static class PageChain
{
    // True when newParent is the page itself or sits in the page's subtree, so moving the page
    // under it would create a cycle.
    public static bool IsSelfOrDescendant(Page page, Page newParent, IReadOnlyList<Page> newParentAncestors)
        => newParent.Id == page.Id || newParentAncestors.Any(ancestor => ancestor.Id == page.Id);

    // Detaches the page from its current chain: the predecessor skips over it, or the chain head
    // passes to the page's next sibling. Any list containing the old siblings (e.g. every page
    // of the notebook) locates the predecessor.
    public static void Unlink(Page page, Notebook notebook, IReadOnlyList<Page> ancestors, IReadOnlyList<Page> oldChain)
    {
        var predecessor = oldChain.FirstOrDefault(candidate => candidate.NextSiblingId == page.Id);
        if (predecessor is not null)
        {
            predecessor.SetNextSibling(page.NextSiblingId);
        }
        else if (page.ParentId is null && notebook.FirstPageId == page.Id)
        {
            notebook.SetFirstPage(page.NextSiblingId);
        }
        else if (ancestors.Count > 0 && ancestors[^1].FirstChildId == page.Id)
        {
            ancestors[^1].SetFirstChild(page.NextSiblingId);
        }
    }

    // Attaches the page into a chain: right after `after`, or at the head when `after` is null.
    // The head lives on the parent (FirstChildId) or the notebook (FirstPageId).
    public static void Link(Page page, Notebook notebook, Page? parent, Page? after)
    {
        if (after is not null)
        {
            after.SetNextSibling(page.Id);
        }
        else if (parent is not null)
        {
            parent.SetFirstChild(page.Id);
        }
        else
        {
            notebook.SetFirstPage(page.Id);
        }
    }

    // Full move: detach from the old chain, attach into the new one at insertIndex, and re-key.
    // The caller resolves the position and checks IsSelfOrDescendant first. Unlinking comes
    // first so that, for a same-parent move where the two chains are one, the pointer fixup
    // stays correct. The page's own FirstChildId is untouched: the child chain moves with it.
    public static void Move(
        Page page,
        Notebook notebook,
        Page? newParent,
        IReadOnlyList<Page> ancestors,
        IReadOnlyList<Page> oldSiblings,
        IReadOnlyList<Page> newSiblings,
        int insertIndex
    )
    {
        Unlink(page, notebook, ancestors, oldSiblings);
        Link(page, notebook, newParent, insertIndex > 0 ? newSiblings[insertIndex - 1] : null);

        var next = insertIndex < newSiblings.Count ? newSiblings[insertIndex] : null;
        page.MoveTo(newParent?.Id, SiblingSortKeys.KeyForInsert(newSiblings, insertIndex), next?.Id);
    }
}
