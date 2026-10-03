using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Tests.Blocks;

public sealed class BlockTests
{
    private static Block NewBlock()
        => Block.Create(Guid.NewGuid(), null, "paragraph", """{"spans":[]}""", string.Empty, SortKeys.First());

    [Fact]
    public void Create_StartsAtVersionOne()
    {
        var block = NewBlock();

        Assert.Equal(1, block.Version);
        Assert.Equal(block.CreatedAtUtc, block.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateContent_ReplacesPayloadAndBumpsVersion()
    {
        var block = NewBlock();

        block.UpdateContent("""{"spans":[{"text":"hi","marks":[]}]}""", "hi");

        Assert.Equal(2, block.Version);
        Assert.Equal("hi", block.PlainText);
        Assert.True(block.UpdatedAtUtc >= block.CreatedAtUtc);
    }

    [Fact]
    public void MarkMoved_BumpsVersionWithoutTouchingPayload()
    {
        var block = NewBlock();
        var contentJson = block.ContentJson;

        block.MarkMoved();

        Assert.Equal(2, block.Version);
        Assert.Equal(contentJson, block.ContentJson);
    }

    [Fact]
    public void Restore_KeepsTheGivenId_AndRestartsTheVersionCounter()
    {
        var id = Guid.CreateVersion7();

        var block = Block.Restore(id, Guid.CreateVersion7(), null, "paragraph", """{"spans":[]}""", string.Empty, "a");

        Assert.Equal(id, block.Id);
        Assert.Equal(1, block.Version);
    }
}
