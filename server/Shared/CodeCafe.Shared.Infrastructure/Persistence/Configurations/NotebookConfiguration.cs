using CodeCafe.Modules.Notes.Domain.Notes;
using CodeCafe.Modules.Identity.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Shared.Infrastructure.Persistence.Configurations;

public sealed class NotebookConfiguration : IEntityTypeConfiguration<Notebook>
{
    public void Configure(EntityTypeBuilder<Notebook> entity)
    {
        entity.ToTable("Notebooks");

        entity.HasKey(notebook => notebook.Id);

        entity.Property(notebook => notebook.Title)
            .HasMaxLength(160)
            .IsRequired();

        entity.Property(notebook => notebook.Slug)
            .HasMaxLength(180)
            .IsRequired();

        entity.Property(notebook => notebook.Description)
            .HasMaxLength(1000);

        entity.Property(notebook => notebook.Visibility)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(notebook => notebook.IsPublished)
            .IsRequired();

        entity.Property(notebook => notebook.CreatedAtUtc)
            .IsRequired();

        entity.Property(notebook => notebook.UpdatedAtUtc);

        entity.Property(notebook => notebook.PublishedAtUtc);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(notebook => notebook.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(notebook => notebook.Items)
            .WithOne(item => item.Notebook)
            .HasForeignKey(item => item.NotebookId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(notebook => notebook.Slug)
            .IsUnique();

        entity.HasIndex(notebook => new { notebook.Visibility, notebook.IsPublished });

        // Trigram index backs the ILIKE '%term%' search in NotebookReadService (PostgreSQL only;
        // other providers ignore the gin method/operator annotations and create a plain index).
        entity.HasIndex(notebook => notebook.Title)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
