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

        builder.HasIndex(notebook => notebook.Slug).IsUnique();

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
