using CodeCafe.Domain.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Shared.Infrastructure.Persistence.Configurations;

public sealed class McpUploadChunkEntryConfiguration : IEntityTypeConfiguration<McpUploadChunkEntry>
{
    public void Configure(EntityTypeBuilder<McpUploadChunkEntry> entity)
    {
        entity.ToTable("McpUploadChunks");

        entity.HasKey(chunk => chunk.Id);

        entity.Property(chunk => chunk.UploadId)
            .HasMaxLength(64)
            .IsRequired();

        entity.Property(chunk => chunk.SequenceNumber)
            .IsRequired();

        entity.Property(chunk => chunk.ContentText)
            .HasColumnType("text")
            .IsRequired();

        entity.Property(chunk => chunk.CreatedAtUtc)
            .IsRequired();

        entity.Property(chunk => chunk.UpdatedAtUtc);

        entity.HasIndex(chunk => new { chunk.UploadId, chunk.SequenceNumber })
            .IsUnique();
    }
}
