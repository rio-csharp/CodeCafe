using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Tests.Blocks;

public sealed class BlockTests
{
    private static Block NewBlock()
        => Block.Create(Guid.NewGuid(), null, "paragraph", """{"spans":[]}""", string.Empty, SortKeys.First());

    [Fact]
    public void Create_StartsAtRevisionOne()
    {
        var block = NewBlock();

        Assert.Equal(1, block.Revision);
        Assert.Equal(block.CreatedAtUtc, block.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateContent_ReplacesPayloadAndBumpsRevision()
    {
        var block = NewBlock();

        block.UpdateContent("""{"spans":[{"text":"hi","marks":[]}]}""", "hi");

        Assert.Equal(2, block.Revision);
        Assert.Equal("hi", block.PlainText);
        Assert.True(block.UpdatedAtUtc >= block.CreatedAtUtc);
    }

    [Fact]
    public void MarkMoved_BumpsRevisionWithoutTouchingPayload()
    {
        var block = NewBlock();
        var contentJson = block.ContentJson;

        block.MarkMoved();

        Assert.Equal(2, block.Revision);
        Assert.Equal(contentJson, block.ContentJson);
    }
}
