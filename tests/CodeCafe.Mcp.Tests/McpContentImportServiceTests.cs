using CodeCafe.Mcp.Configuration;
using CodeCafe.Mcp.Tools.Notes;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CodeCafe.Mcp.Tests;

public sealed class McpContentImportServiceTests
{
    [Fact]
    public async Task ResolveRequiredPageContentAsync_RejectsInlineH1Headings()
    {
        var service = CreateService();
        using var document = JsonDocument.Parse("""
            {
              "type": "doc",
              "content": [
                {
                  "type": "heading",
                  "attrs": { "level": 1 },
                  "content": [{ "type": "text", "text": "Body title" }]
                }
              ]
            }
            """);

        var result = await service.ResolveRequiredPageContentAsync(
            Guid.NewGuid(),
            document.RootElement,
            contentUploadId: null,
            contentFormat: null,
            "invalid_content_json",
            "invalid content",
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_content_json", result.Error!.Code);
        Assert.Equal("contentJson", result.Error.Field);
        Assert.Equal("heading", result.Error.Details!["disallowedNodeType"]);
        Assert.Equal(1, result.Error.Details["disallowedHeadingLevel"]);
        Assert.Equal("$.content[0]", result.Error.Details["nodePath"]);
    }

    [Fact]
    public async Task ResolveRequiredBlocksAsync_RejectsInlineH1Headings()
    {
        var service = CreateService();
        using var blocks = JsonDocument.Parse("""
            [
              {
                "type": "heading",
                "attrs": { "level": 1 },
                "content": [{ "type": "text", "text": "Append title" }]
              }
            ]
            """);

        var result = await service.ResolveRequiredBlocksAsync(
            Guid.NewGuid(),
            blocks.RootElement,
            blocksUploadId: null,
            blocksFormat: null,
            "invalid_blocks",
            "invalid blocks",
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_blocks", result.Error!.Code);
        Assert.Equal("blocks", result.Error.Field);
        Assert.Equal("$[0]", result.Error.Details!["nodePath"]);
    }

    [Fact]
    public async Task ResolveRequiredPageContentAsync_DemotesUploadedMarkdownH1Headings()
    {
        var actorId = Guid.NewGuid();
        var upload = new McpUploadSession(
            "upload-1",
            actorId,
            "page.md",
            "text/markdown",
            "# Page title",
            12,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var service = CreateService(new TestUploadStore(upload));

        var result = await service.ResolveRequiredPageContentAsync(
            actorId,
            inlineContentJson: null,
            contentUploadId: upload.UploadId,
            contentFormat: "markdown",
            "invalid_content_json",
            "invalid content",
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var heading = result.Value.GetProperty("content")[0];
        Assert.Equal("heading", heading.GetProperty("type").GetString());
        Assert.Equal(2, heading.GetProperty("attrs").GetProperty("level").GetInt32());
    }

    private static McpContentImportService CreateService()
        => new(
            new TestUploadStore(),
            new MarkdigMcpMarkdownImporter(),
            Options.Create(new McpOptions()));

    private static McpContentImportService CreateService(IMcpUploadStore uploadStore)
        => new(
            uploadStore,
            new MarkdigMcpMarkdownImporter(),
            Options.Create(new McpOptions()));

    private sealed class TestUploadStore(McpUploadSession? session = null) : IMcpUploadStore
    {
        public Task<McpUploadStatus> CreateAsync(Guid actorId, string? fileName, string mediaType, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<NotesUploadResult<McpUploadStatus>> AppendTextAsync(
            Guid actorId,
            string uploadId,
            string chunkText,
            int maxChunkBytes,
            int maxUploadBytes,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<NotesUploadResult<McpUploadSession>> GetAsync(Guid actorId, string uploadId, CancellationToken cancellationToken)
            => Task.FromResult(
                session is not null && session.ActorId == actorId && session.UploadId == uploadId
                    ? NotesUploadResult<McpUploadSession>.Success(session)
                    : NotesUploadResult<McpUploadSession>.Failure("upload_not_found", "Upload session was not found."));

        public Task<bool> DeleteAsync(Guid actorId, string uploadId, CancellationToken cancellationToken)
            => Task.FromResult(false);
    }
}
