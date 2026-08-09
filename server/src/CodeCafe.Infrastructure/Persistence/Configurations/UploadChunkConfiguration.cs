using CodeCafe.Domain.Uploads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps UploadChunk to the McpUploadChunks table for backward compatibility.
/// The table retains its MCP prefix even though uploads are now a general-purpose feature.
/// </summary>
public sealed class UploadChunkConfiguration : IEntityTypeConfiguration<UploadChunk>
{
    public void Configure(EntityTypeBuilder<UploadChunk> entity)
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
