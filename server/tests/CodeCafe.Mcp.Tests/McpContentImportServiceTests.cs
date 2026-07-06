using CodeCafe.Mcp.Configuration;
using CodeCafe.Mcp.Tools.Notes;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CodeCafe.Mcp.Tests;

public sealed class McpContentImportServiceTests
{
    [Fact]
    public async Task ResolveRequiredPageContentAsync_AllowsInlineHeadingNodes()
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

        Assert.True(result.Succeeded);
        var heading = result.Value.GetProperty("content")[0];
        Assert.Equal("heading", heading.GetProperty("type").GetString());
        Assert.Equal(1, heading.GetProperty("attrs").GetProperty("level").GetInt32());
    }

    [Fact]
    public async Task ResolveRequiredBlocksAsync_AllowsInlineHeadingNodes()
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

        Assert.True(result.Succeeded);
        var heading = result.Value[0];
        Assert.Equal("heading", heading.GetProperty("type").GetString());
        Assert.Equal(1, heading.GetProperty("attrs").GetProperty("level").GetInt32());
    }

    [Fact]
    public async Task ResolveRequiredPageContentAsync_PreservesUploadedMarkdownHeadingNodes()
    {
        var actorId = Guid.NewGuid();
        var upload = new McpUploadSession(
            "upload-1",
            actorId,
            "page.md",
            "text/markdown",
            "# Page title\n\nFirst paragraph.",
            30,
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
        var firstNode = result.Value.GetProperty("content")[0];
        Assert.Equal("heading", firstNode.GetProperty("type").GetString());
        Assert.Equal(1, firstNode.GetProperty("attrs").GetProperty("level").GetInt32());
        Assert.Equal(
            "Page title",
            firstNode.GetProperty("content")[0].GetProperty("text").GetString());
        var secondNode = result.Value.GetProperty("content")[1];
        Assert.Equal("paragraph", secondNode.GetProperty("type").GetString());
        Assert.Equal(
            "First paragraph.",
            secondNode.GetProperty("content")[0].GetProperty("text").GetString());
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

        public Task<NotesUploadResult<McpUploadStatus>> CreateTextAsync(
            Guid actorId,
            string? fileName,
            string mediaType,
            string contentText,
            int maxUploadBytes,
            CancellationToken cancellationToken)
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
