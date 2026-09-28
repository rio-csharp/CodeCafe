using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("pages");

        builder.HasKey(page => page.Id);

        builder.Property(page => page.Title).HasMaxLength(Page.MaxTitleLength);
        builder.Property(page => page.Slug).HasMaxLength(Page.MaxSlugLength);
        builder.Property(page => page.SortKey).HasMaxLength(Page.MaxSortKeyLength);

        builder.HasQueryFilter(page => page.DeletedAtUtc == null);

        // Partial, mirroring notebooks: a trashed page stops reserving its slug.
        builder.HasIndex(page => new { page.NotebookId, page.Slug }).IsUnique().HasFilter("\"DeletedAtUtc\" IS NULL");

        // Sibling listings order by SortKey; the chain pointers get no index because reads never walk them.
        builder.HasIndex(page => new { page.NotebookId, page.ParentId, page.SortKey });

        builder
            .HasOne<Notebook>()
            .WithMany()
            .HasForeignKey(page => page.NotebookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(page => page.Shares)
            .WithOne()
            .HasForeignKey(share => share.PageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
