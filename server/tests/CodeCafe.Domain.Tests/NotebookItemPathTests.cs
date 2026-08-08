using CodeCafe.Domain.Notes;

namespace CodeCafe.Domain.Tests;

/// <summary>
/// Slug and path length budgeting. A generated slug must never exceed the Slug column (180) and a
/// materialised path must never exceed the Path column (1024); before these guards existed, a long
/// title or deep nesting produced a database error surfacing as a 500.
/// </summary>
public sealed class NotebookItemPathTests
{
    private const int MaxTitleLength = 160;

    [Fact]
    public void FromTitle_Truncates_Slug_To_Column_Budget()
    {
        var title = new string('a', MaxTitleLength);

        var slug = NotebookSlugGenerator.FromTitle(title, "page");

        Assert.True(slug.Length <= NotebookSlugGenerator.MaxSlugLength);
    }

    [Fact]
    public void WithUniqueSuffix_Stays_Within_Budget_For_Max_Length_Title()
    {
        // The pre-fix fallback was $"{baseSlug}-{Guid:N}", i.e. up to 160 + 1 + 32 = 193 characters
        // against a 180-character column.
        var baseSlug = NotebookSlugGenerator.FromTitle(new string('a', MaxTitleLength), "page");

        var slug = NotebookSlugGenerator.WithUniqueSuffix(baseSlug, NotebookSlugGenerator.MaxSlugLength);

        Assert.True(
            slug.Length <= NotebookSlugGenerator.MaxSlugLength,
            $"Expected at most {NotebookSlugGenerator.MaxSlugLength} characters, got {slug.Length}.");
    }

    [Fact]
    public void WithUniqueSuffix_Keeps_A_Random_Tail_For_Long_Base_Slugs()
    {
        var baseSlug = new string('a', NotebookSlugGenerator.MaxSlugLength);

        var first = NotebookSlugGenerator.WithUniqueSuffix(baseSlug, NotebookSlugGenerator.MaxSlugLength);
        var second = NotebookSlugGenerator.WithUniqueSuffix(baseSlug, NotebookSlugGenerator.MaxSlugLength);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void WithSuffix_Preserves_Suffix_By_Shortening_The_Base_Slug()
    {
        var baseSlug = new string('a', NotebookSlugGenerator.MaxSlugLength);

        var slug = NotebookSlugGenerator.WithSuffix(baseSlug, 7);

        Assert.True(slug.Length <= NotebookSlugGenerator.MaxSlugLength);
        Assert.EndsWith("-7", slug);
    }

    [Fact]
    public void GetSlugBudget_Shrinks_As_Parent_Path_Grows()
    {
        var deepParent = new string('a', NotebookItemPath.MaxPathLength - 40);

        var budget = NotebookItemPath.GetSlugBudget(deepParent);

        Assert.Equal(39, budget);
    }

    [Fact]
    public void HasRoomForChild_Is_False_When_Parent_Path_Leaves_No_Usable_Slug_Space()
    {
        var nearLimitParent = new string('a', NotebookItemPath.MaxPathLength - 4);

        Assert.False(NotebookItemPath.HasRoomForChild(nearLimitParent));
    }

    [Fact]
    public void HasRoomForChild_Is_True_At_Root()
    {
        Assert.True(NotebookItemPath.HasRoomForChild(null));
        Assert.True(NotebookItemPath.HasRoomForChild(string.Empty));
    }

    [Fact]
    public void GeneratePath_Stays_Within_Path_Budget_For_Deeply_Nested_Parent()
    {
        var parentPath = new string('a', NotebookItemPath.MaxPathLength - 60);
        var title = new string('b', MaxTitleLength);

        var path = NotebookItemTree.GeneratePath([], parentPath, title, Guid.NewGuid());

        Assert.True(
            path.Length <= NotebookItemPath.MaxPathLength,
            $"Expected at most {NotebookItemPath.MaxPathLength} characters, got {path.Length}.");
    }

    [Fact]
    public void GeneratePath_Stays_Within_Budget_When_Falling_Back_After_Conflicts()
    {
        var parentPath = new string('a', NotebookItemPath.MaxPathLength - 60);
        var title = new string('b', MaxTitleLength);
        var slugBudget = NotebookItemPath.GetSlugBudget(parentPath);
        var baseSlug = NotebookSlugGenerator.FromTitle(title, "page", slugBudget);
        var existing = Enumerable.Range(0, 10)
            .Select(attempt => CreateItem(
                Guid.NewGuid(),
                $"{parentPath}/{NotebookSlugGenerator.WithSuffix(baseSlug, attempt, slugBudget)}"))
            .ToArray();

        var path = NotebookItemTree.GeneratePath(existing, parentPath, title, Guid.NewGuid());

        Assert.True(
            path.Length <= NotebookItemPath.MaxPathLength,
            $"Expected at most {NotebookItemPath.MaxPathLength} characters, got {path.Length}.");
        Assert.DoesNotContain(existing, item => item.Path == path);
    }

    [Fact]
    public void GeneratePath_Throws_When_Parent_Has_No_Room()
    {
        // Callers are expected to reject this as a validation failure via HasRoomForChild; the throw
        // is the backstop that keeps an over-long path from reaching the database.
        var nearLimitParent = new string('a', NotebookItemPath.MaxPathLength - 4);

        Assert.Throws<ArgumentException>(
            () => NotebookItemTree.GeneratePath([], nearLimitParent, "Page", Guid.NewGuid()));
    }

    [Fact]
    public void DescendantsFitAfterMove_Is_False_When_Rewrite_Overflows_A_Descendant()
    {
        var folderId = Guid.NewGuid();
        var oldPath = "f";
        var deepDescendant = CreateItem(Guid.NewGuid(), $"{oldPath}/{new string('c', NotebookItemPath.MaxPathLength - 10)}");
        var items = new[] { CreateItem(folderId, oldPath), deepDescendant };
        var newPath = new string('n', 40);

        Assert.False(NotebookItemPath.DescendantsFitAfterMove(items, folderId, oldPath, newPath));
    }

    [Fact]
    public void DescendantsFitAfterMove_Is_True_When_Path_Shrinks()
    {
        var folderId = Guid.NewGuid();
        var oldPath = new string('o', 100);
        var deepDescendant = CreateItem(Guid.NewGuid(), $"{oldPath}/{new string('c', NotebookItemPath.MaxPathLength - 200)}");
        var items = new[] { CreateItem(folderId, oldPath), deepDescendant };

        Assert.True(NotebookItemPath.DescendantsFitAfterMove(items, folderId, oldPath, "short"));
    }

    [Fact]
    public void DescendantsFitAfterMove_Ignores_Items_Outside_The_Subtree()
    {
        var folderId = Guid.NewGuid();
        var oldPath = "f";
        // Same prefix character but not a descendant of "f/".
        var unrelated = CreateItem(Guid.NewGuid(), new string('f', NotebookItemPath.MaxPathLength - 2));
        var items = new[] { CreateItem(folderId, oldPath), unrelated };

        Assert.True(NotebookItemPath.DescendantsFitAfterMove(items, folderId, oldPath, new string('n', 40)));
    }

    private static NotebookItem CreateItem(Guid id, string path)
    {
        return new NotebookItem
        {
            Id = id,
            NotebookId = Guid.NewGuid(),
            Type = NotebookItemType.Page,
            Title = path.Split('/')[^1],
            Slug = path.Split('/')[^1],
            Path = path
        };
    }
}
