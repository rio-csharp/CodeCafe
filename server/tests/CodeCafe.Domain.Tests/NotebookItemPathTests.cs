using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.Services;
using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Domain.Tests;

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
        var baseSlug = NotebookSlugGenerator.FromTitle(new string('a', MaxTitleLength), "page");

        var slug = NotebookSlugGenerator.WithUniqueSuffix(
            baseSlug,
            NotebookSlugGenerator.MaxSlugLength
        );

        Assert.True(
            slug.Length <= NotebookSlugGenerator.MaxSlugLength,
            $"Expected at most {NotebookSlugGenerator.MaxSlugLength} characters, got {slug.Length}."
        );
    }

    [Fact]
    public void WithUniqueSuffix_Keeps_A_Random_Tail_For_Long_Base_Slugs()
    {
        var baseSlug = new string('a', NotebookSlugGenerator.MaxSlugLength);

        var first = NotebookSlugGenerator.WithUniqueSuffix(
            baseSlug,
            NotebookSlugGenerator.MaxSlugLength
        );
        var second = NotebookSlugGenerator.WithUniqueSuffix(
            baseSlug,
            NotebookSlugGenerator.MaxSlugLength
        );

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
        var deepParent = new string('a', NotebookPath.MaxLength - 40);

        var budget = NotebookPath.GetSlugBudget(deepParent);

        Assert.Equal(39, budget);
    }

    [Fact]
    public void HasRoomForChild_Is_False_When_Parent_Path_Leaves_No_Usable_Slug_Space()
    {
        var nearLimitParent = new string('a', NotebookPath.MaxLength - 4);

        Assert.False(NotebookPath.HasRoomForChild(nearLimitParent));
    }

    [Fact]
    public void HasRoomForChild_Is_True_At_Root()
    {
        Assert.True(NotebookPath.HasRoomForChild(null));
        Assert.True(NotebookPath.HasRoomForChild(string.Empty));
    }

    [Fact]
    public void GeneratePath_Stays_Within_Path_Budget_For_Deeply_Nested_Parent()
    {
        var parentPath = new string('a', NotebookPath.MaxLength - 60);
        var title = new string('b', MaxTitleLength);

        var path = NotebookItemTree.GeneratePath([], parentPath, title, Guid.NewGuid());

        Assert.True(
            path.Length <= NotebookPath.MaxLength,
            $"Expected at most {NotebookPath.MaxLength} characters, got {path.Length}."
        );
    }

    [Fact]
    public void GeneratePath_Stays_Within_Budget_When_Falling_Back_After_Conflicts()
    {
        var parentPath = new string('a', NotebookPath.MaxLength - 60);
        var title = new string('b', MaxTitleLength);
        var slugBudget = NotebookPath.GetSlugBudget(parentPath);
        var baseSlug = NotebookSlugGenerator.FromTitle(title, "page", slugBudget);
        var existing = Enumerable
            .Range(0, 10)
            .Select(attempt =>
                CreateItem(
                    Guid.NewGuid(),
                    $"{parentPath}/{NotebookSlugGenerator.WithSuffix(baseSlug, attempt, slugBudget)}"
                )
            )
            .ToArray();

        var path = NotebookItemTree.GeneratePath(existing, parentPath, title, Guid.NewGuid());

        Assert.True(
            path.Length <= NotebookPath.MaxLength,
            $"Expected at most {NotebookPath.MaxLength} characters, got {path.Length}."
        );
        Assert.DoesNotContain(existing, item => item.Path.Value == path);
    }

    [Fact]
    public void GeneratePath_Throws_When_Parent_Has_No_Room()
    {
        var nearLimitParent = new string('a', NotebookPath.MaxLength - 4);

        Assert.Throws<ArgumentException>(() =>
            NotebookItemTree.GeneratePath([], nearLimitParent, "Page", Guid.NewGuid())
        );
    }

    [Fact]
    public void DescendantsFitAfterMove_Is_False_When_Rewrite_Overflows_A_Descendant()
    {
        var folderId = Guid.NewGuid();
        var oldPath = "f";
        var deepDescendant = CreateItem(
            Guid.NewGuid(),
            $"{oldPath}/{new string('c', NotebookPath.MaxLength - 10)}"
        );
        var items = new[] { CreateItem(folderId, oldPath), deepDescendant };
        var newPath = new string('n', 40);

        Assert.False(NotebookItemTree.DescendantsFitAfterMove(items, folderId, oldPath, newPath));
    }

    [Fact]
    public void DescendantsFitAfterMove_Is_True_When_Path_Shrinks()
    {
        var folderId = Guid.NewGuid();
        var oldPath = new string('o', 100);
        var deepDescendant = CreateItem(
            Guid.NewGuid(),
            $"{oldPath}/{new string('c', NotebookPath.MaxLength - 200)}"
        );
        var items = new[] { CreateItem(folderId, oldPath), deepDescendant };

        Assert.True(NotebookItemTree.DescendantsFitAfterMove(items, folderId, oldPath, "short"));
    }

    [Fact]
    public void DescendantsFitAfterMove_Ignores_Items_Outside_The_Subtree()
    {
        var folderId = Guid.NewGuid();
        var oldPath = "f";
        var unrelated = CreateItem(Guid.NewGuid(), new string('f', NotebookPath.MaxLength - 2));
        var items = new[] { CreateItem(folderId, oldPath), unrelated };

        Assert.True(
            NotebookItemTree.DescendantsFitAfterMove(items, folderId, oldPath, new string('n', 40))
        );
    }

    private static NotebookItem CreateItem(Guid id, string path)
    {
        var segment = path.Split('/')[^1];
        var slug = segment[..Math.Min(segment.Length, NotebookSlug.MaxLength)];
        return NotebookItem.Create(
            id,
            Guid.NewGuid(),
            null,
            NotebookItemType.Page,
            segment,
            NotebookSlug.Create(slug),
            NotebookPath.Create(path),
            0,
            DateTimeOffset.UtcNow
        );
    }
}
