using System.Text.Json;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common.Markdown;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Tests.Common;

// The importer is the one writer that builds payloads itself instead of taking them from a
// request, so it has to run them through the same write-side authority as every other path.
public sealed class MarkdownBlockImporterTests
{
    [Fact]
    public void CreateBlocks_InvalidPayload_FailsWithoutWriting()
    {
        var page = Page.Create(Guid.CreateVersion7(), null, "Page", "page", "a");
        var blocks = new StubBlockRepository();
        var revisions = new StubBlockRevisionRepository();
        var invalid = Parsed(BlockTypes.Table, """{"alignments":[],"header":null,"rows":[]}""");

        var result = MarkdownBlockImporter.CreateBlocks(
            page,
            [invalid],
            parent: null,
            Guid.CreateVersion7(),
            blocks,
            revisions,
            RevisionSource.Human
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPayload.Code, result.Error!.Code);
        Assert.Empty(blocks);
        Assert.Empty(revisions);
    }

    [Fact]
    public void CreateBlocks_UsesTheCanonicalPayloadAndItsPlainText()
    {
        var page = Page.Create(Guid.CreateVersion7(), null, "Page", "page", "a");
        var blocks = new StubBlockRepository();
        var revisions = new StubBlockRevisionRepository();
        // The parser's projection is stale on purpose: normalization is the authority.
        var heading = Parsed(BlockTypes.Heading, """{"level":2,"spans":[{"text":" Section ","marks":[]}]}""");

        var result = MarkdownBlockImporter.CreateBlocks(
            page,
            [heading],
            parent: null,
            Guid.CreateVersion7(),
            blocks,
            revisions,
            RevisionSource.Human
        );

        Assert.True(result.IsSuccess, result.Error?.Message);
        var block = Assert.Single(blocks);
        Assert.Equal(" Section ", block.PlainText);
        Assert.Equal(
            """{"level":2,"spans":[{"text":" Section ","marks":[]}]}""",
            block.ContentJson
        );
        Assert.Single(revisions);
    }

    private static ParsedMarkdownBlock Parsed(string type, string payloadJson)
        => new(type, JsonDocument.Parse(payloadJson).RootElement.Clone(), "parser projection", []);
}
