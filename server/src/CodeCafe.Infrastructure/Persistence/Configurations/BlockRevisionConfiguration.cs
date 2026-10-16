using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class BlockRevisionConfiguration : IEntityTypeConfiguration<BlockRevision>
{
    public void Configure(EntityTypeBuilder<BlockRevision> builder)
    {
        builder.ToTable("block_revisions");

        builder.HasKey(revision => revision.Id);

        builder.Property(revision => revision.ChangeKind).HasConversion<string>().HasMaxLength(16);
        builder.Property(revision => revision.Source).HasConversion<string>().HasMaxLength(16);
        builder.Property(revision => revision.Type).HasMaxLength(64);
        builder.Property(revision => revision.SortKey).HasMaxLength(Block.MaxSortKeyLength);
        builder.Property(revision => revision.ContentJson).HasColumnType("jsonb");
        builder.Property(revision => revision.PlainText).HasColumnType("text");
        builder.Property(revision => revision.SubtreeJson).HasColumnType("jsonb");

        // Keyset pagination (newest first) for the two read endpoints.
        builder.HasIndex(revision => new { revision.PageId, revision.CreatedAtUtc, revision.Id }).IsDescending();
        builder.HasIndex(revision => new { revision.PageId, revision.BlockId, revision.CreatedAtUtc, revision.Id }).IsDescending();

        // Deliberately NO foreign key to blocks: rows must outlive their hard-deleted block.
        // Page purge takes the page's whole history with it.
        builder
            .HasOne<Page>()
            .WithMany()
            .HasForeignKey(revision => revision.PageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
