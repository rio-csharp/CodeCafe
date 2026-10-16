using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class NotebookConfiguration : IEntityTypeConfiguration<Notebook>
{
    public void Configure(EntityTypeBuilder<Notebook> builder)
    {
        builder.ToTable("notebooks");

        builder.HasKey(notebook => notebook.Id);

        builder.Property(notebook => notebook.Title).HasMaxLength(Notebook.MaxTitleLength).IsRequired();

        builder.Property(notebook => notebook.Description).HasMaxLength(Notebook.MaxDescriptionLength);

        builder.Property(notebook => notebook.Slug).HasMaxLength(Notebook.MaxSlugLength).IsRequired();

        // Partial so a trashed notebook stops reserving its slug. A full index would keep the row
        // reserved while the availability check (which honours the soft-delete filter) reports the
        // slug as free, so a create could never recover from the conflict.
        builder.HasIndex(notebook => notebook.Slug).IsUnique().HasFilter("\"DeletedAtUtc\" IS NULL");

        // Search matches with ILIKE '%q%', which only stays index-backed through trigrams.
        builder.HasIndex(notebook => notebook.Title).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(notebook => notebook.Description).HasMethod("gin").HasOperators("gin_trgm_ops");

        builder.Property(notebook => notebook.Visibility).HasConversion<string>().HasMaxLength(16);

        builder.PrimitiveCollection(notebook => notebook.Tags).HasColumnType("text[]");

        builder.Property(notebook => notebook.AccessCodeHash).HasMaxLength(128);

        builder
            .HasMany(notebook => notebook.Shares)
            .WithOne()
            .HasForeignKey(share => share.NotebookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(notebook => notebook.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Trashed notebooks are hidden everywhere; trash queries opt out via IgnoreQueryFilters.
        builder.HasQueryFilter(notebook => notebook.DeletedAtUtc == null);

        builder.Navigation(notebook => notebook.Shares).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
