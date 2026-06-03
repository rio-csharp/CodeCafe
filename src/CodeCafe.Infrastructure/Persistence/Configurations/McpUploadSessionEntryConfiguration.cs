using CodeCafe.Domain.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class McpUploadSessionEntryConfiguration : IEntityTypeConfiguration<McpUploadSessionEntry>
{
    public void Configure(EntityTypeBuilder<McpUploadSessionEntry> entity)
    {
        entity.ToTable("McpUploadSessions");

        entity.HasKey(session => session.UploadId);

        entity.Property(session => session.UploadId)
            .HasMaxLength(64)
            .ValueGeneratedNever();

        entity.Property(session => session.ActorUserId)
            .IsRequired();

        entity.Property(session => session.FileName)
            .HasMaxLength(512);

        entity.Property(session => session.MediaType)
            .HasMaxLength(160)
            .IsRequired();

        entity.Property(session => session.BytesReceived)
            .IsRequired();

        entity.Property(session => session.ChunkCount)
            .IsRequired();

        entity.Property(session => session.CreatedAtUtc)
            .IsRequired();

        entity.Property(session => session.UpdatedAtUtc);

        entity.HasMany(session => session.Chunks)
            .WithOne(chunk => chunk.UploadSession)
            .HasForeignKey(chunk => chunk.UploadId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(session => new { session.ActorUserId, session.UpdatedAtUtc });
    }
}
