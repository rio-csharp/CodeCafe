using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class NotebookItemConfiguration : IEntityTypeConfiguration<NotebookItem>
{
    public void Configure(EntityTypeBuilder<NotebookItem> entity)
    {
        entity.ToTable("NotebookItems");

        entity.HasKey(item => item.Id);

        entity.Property(x => x.Id).ValueGeneratedNever();

        entity.Ignore(item => item.DomainEvents);

        // Npgsql convention: uint + rowversion maps to the xmin system column (see Npgsql EF docs).
        entity.Property<uint>("xmin").IsRowVersion();

        entity.Property(item => item.Type).HasConversion<string>().HasMaxLength(20).IsRequired();

        entity.Property(item => item.Title).HasMaxLength(160).IsRequired();

        entity
            .Property(item => item.Slug)
            .HasConversion(slug => slug.Value, value => NotebookSlug.Create(value))
            .HasMaxLength(NotebookSlug.MaxLength)
            .IsRequired();

        entity
            .Property(item => item.Path)
            .HasConversion(path => path.Value, value => NotebookPath.Create(value))
            .HasMaxLength(NotebookPath.MaxLength)
            .IsRequired();

        entity.Property(item => item.SortOrder).IsRequired();

        entity.Property(item => item.ContentJson).HasColumnType("jsonb");

        entity.Property(item => item.PlainTextContent);

        entity.Property(item => item.IsArchived).IsRequired();

        entity.Property(item => item.ArchivedAtUtc);

        entity.Property(item => item.ArchivedByUserId);

        entity.Property(item => item.CreatedAtUtc).IsRequired();

        entity.Property(item => item.UpdatedAtUtc);

        entity
            .HasOne<NotebookItem>()
            .WithMany()
            .HasForeignKey(item => item.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(item => new { item.NotebookId, item.Path }).IsUnique();

        entity.HasIndex(item => new
        {
            item.NotebookId,
            item.ParentId,
            item.SortOrder,
        });
        entity.HasIndex(item => new { item.NotebookId, item.IsArchived });

        entity.HasIndex(item => item.Title).HasMethod("gin").HasOperators("gin_trgm_ops");

        entity
            .HasIndex(item => item.PlainTextContent)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
