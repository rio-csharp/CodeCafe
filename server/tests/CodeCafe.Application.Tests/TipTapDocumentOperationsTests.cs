using CodeCafe.Application.Notes;
using System.Text.Json;

namespace CodeCafe.Application.Tests;

public sealed class TipTapDocumentOperationsTests
{
    [Fact]
    public void ReplaceTextInBlock_OnlyMutatesRequestedBlock()
    {
        var document = JsonSerializer.SerializeToElement(new
        {
            type = "doc",
            content = new object[]
            {
                new
                {
                    type = "paragraph",
                    content = new[] { new { type = "text", text = "Overview content" } }
                },
                new
                {
                    type = "paragraph",
                    content = new[] { new { type = "text", text = "Overview content" } }
                }
            }
        });

        var updated = TipTapDocumentOperations.ApplyOperations(
            document,
            JsonSerializer.SerializeToElement(new object[]
            {
                new
                {
                    type = "replace_text_in_block",
                    index = 1,
                    searchText = "Overview",
                    replacementText = "Rollout",
                    replaceAll = false
                }
            }));

        var blocks = updated.GetProperty("content").EnumerateArray().ToArray();
        Assert.Equal("Overview content", blocks[0].GetProperty("content")[0].GetProperty("text").GetString());
        Assert.Equal("Rollout content", blocks[1].GetProperty("content")[0].GetProperty("text").GetString());
    }
}
