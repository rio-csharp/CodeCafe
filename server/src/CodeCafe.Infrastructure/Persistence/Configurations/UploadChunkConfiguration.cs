using CodeCafe.Domain.Uploads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class UploadChunkConfiguration : IEntityTypeConfiguration<UploadChunk>
{
    public void Configure(EntityTypeBuilder<UploadChunk> entity)
    {
        entity.ToTable("UploadChunks");

        entity.HasKey(chunk => chunk.Id);

        entity.Property(x => x.Id).ValueGeneratedNever();

        entity.Ignore(chunk => chunk.DomainEvents);

        entity.Property(chunk => chunk.UploadId).IsRequired();

        entity.Property(chunk => chunk.SequenceNumber).IsRequired();

        entity.Property(chunk => chunk.ContentText).HasColumnType("text").IsRequired();

        entity.Property(chunk => chunk.CreatedAtUtc).IsRequired();

        entity.Property(chunk => chunk.UpdatedAtUtc);

        entity.HasIndex(chunk => new { chunk.UploadId, chunk.SequenceNumber }).IsUnique();
    }
}
