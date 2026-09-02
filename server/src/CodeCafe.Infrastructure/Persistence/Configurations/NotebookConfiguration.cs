using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.ValueObjects;
using CodeCafe.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class NotebookConfiguration : IEntityTypeConfiguration<Notebook>
{
    public void Configure(EntityTypeBuilder<Notebook> entity)
    {
        entity.ToTable("Notebooks");

        entity.HasKey(notebook => notebook.Id);

        entity.Property(x => x.Id).ValueGeneratedNever();

        entity.Ignore(notebook => notebook.DomainEvents);

        entity.Property(notebook => notebook.Title).HasMaxLength(160).IsRequired();

        entity
            .Property(notebook => notebook.Slug)
            .HasConversion(slug => slug.Value, value => NotebookSlug.Create(value))
            .HasMaxLength(NotebookSlug.MaxLength)
            .IsRequired();

        entity.Property(notebook => notebook.Description).HasMaxLength(1000);

        entity
            .Property(notebook => notebook.Visibility)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(notebook => notebook.CreatedAtUtc).IsRequired();

        entity.Property(notebook => notebook.UpdatedAtUtc);

        entity.Property(notebook => notebook.AccessCodeHash).HasMaxLength(200);

        entity.Property(notebook => notebook.PublishedAtUtc);

        entity
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(notebook => notebook.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasMany(notebook => notebook.Items)
            .WithOne()
            .HasForeignKey(item => item.NotebookId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(notebook => notebook.Slug).IsUnique();

        entity.HasIndex(notebook => notebook.Visibility);

        entity.HasIndex(notebook => notebook.Title).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
