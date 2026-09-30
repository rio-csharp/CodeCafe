using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("blocks");

        builder.HasKey(block => block.Id);

        builder.Property(block => block.ContentJson).HasColumnType("jsonb");
        builder.Property(block => block.PlainText).HasColumnType("text");
        builder.Property(block => block.SortKey).HasMaxLength(Block.MaxSortKeyLength);
        builder.Property(block => block.Revision).IsConcurrencyToken();

        // Ordered sibling reads; covers PageId-only scans as the leftmost prefix.
        builder.HasIndex(block => new { block.PageId, block.ParentBlockId, block.SortKey });

        // Search matches PlainText with ILIKE '%q%', which only stays index-backed through trigrams.
        builder.HasIndex(block => block.PlainText).HasMethod("gin").HasOperators("gin_trgm_ops");

        builder
            .HasOne<Page>()
            .WithMany()
            .HasForeignKey(block => block.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK asymmetry is deliberate: purging a page or deleting a subtree root cascades through
        // PageId/ParentBlockId, but the chain pointers RESTRICT — cascade never repairs a linked
        // list, so a delete that forgot to unlink must fail loudly instead of silently corrupting
        // the chain. BlockConfiguration configures all four FKs, PageConfiguration the fifth.
        builder
            .HasOne<Block>()
            .WithMany()
            .HasForeignKey(block => block.ParentBlockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<Block>()
            .WithMany()
            .HasForeignKey(block => block.NextSiblingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<Block>()
            .WithMany()
            .HasForeignKey(block => block.FirstChildId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
