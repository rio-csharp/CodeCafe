using CodeCafe.Infrastructure.Uploads;
using CodeCafe.Infrastructure.Mcp;
using CodeCafe.Application.Common.Uploads;
using CodeCafe.Application.Mcp;
using CodeCafe.Host.Mcp;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Common.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CodeCafe.Host.Mcp.Tests;

public sealed class MarkdownContentImporterTests
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
        var upload = new UploadSession(
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

    private static MarkdownContentImporter CreateService()
        => new(
            new TestUploadStore(),
            new MarkdigMcpMarkdownImporter(),
            Options.Create(new McpOptions()));

    private static MarkdownContentImporter CreateService(IUploadStore uploadStore)
        => new(
            uploadStore,
            new MarkdigMcpMarkdownImporter(),
            Options.Create(new McpOptions()));

    private sealed class TestUploadStore(UploadSession? session = null) : IUploadStore
    {
        public Task<UploadStatus> CreateAsync(Guid actorId, string? fileName, string mediaType, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<UploadResult<UploadStatus>> CreateTextAsync(
            Guid actorId,
            string? fileName,
            string mediaType,
            string contentText,
            int maxUploadBytes,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<UploadResult<UploadStatus>> AppendTextAsync(
            Guid actorId,
            string uploadId,
            string chunkText,
            int maxChunkBytes,
            int maxUploadBytes,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<UploadResult<UploadSession>> GetAsync(Guid actorId, string uploadId, CancellationToken cancellationToken)
            => Task.FromResult(
                session is not null && session.ActorId == actorId && session.UploadId == uploadId
                    ? UploadResult<UploadSession>.Success(session)
                    : UploadResult<UploadSession>.Failure("upload_not_found", "Upload session was not found."));

        public Task<bool> DeleteAsync(Guid actorId, string uploadId, CancellationToken cancellationToken)
            => Task.FromResult(false);
    }
}
