using CodeCafe.Domain.Notes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class NotebookItemConfiguration : IEntityTypeConfiguration<NotebookItem>
{
    public void Configure(EntityTypeBuilder<NotebookItem> entity)
    {
        entity.ToTable("NotebookItems");

        entity.HasKey(item => item.Id);

        entity.Property(item => item.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(item => item.Title)
            .HasMaxLength(160)
            .IsRequired();

        entity.Property(item => item.Slug)
            .HasMaxLength(180)
            .IsRequired();

        entity.Property(item => item.Path)
            .HasMaxLength(1024)
            .IsRequired();

        entity.Property(item => item.SortOrder)
            .IsRequired();

        entity.Property(item => item.ContentFormat)
            .HasMaxLength(40);

        entity.Property(item => item.ContentJson)
            .HasColumnType("jsonb");

        entity.Property(item => item.PlainTextContent);

        entity.Property(item => item.Revision)
            .IsConcurrencyToken()
            .IsRequired();

        entity.Property(item => item.IsArchived)
            .IsRequired();

        entity.Property(item => item.ArchivedAtUtc);

        entity.Property(item => item.ArchivedByUserId);

        entity.Property(item => item.CreatedAtUtc)
            .IsRequired();

        entity.Property(item => item.UpdatedAtUtc);

        entity.HasOne(item => item.Parent)
            .WithMany(parent => parent.Children)
            .HasForeignKey(item => item.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(item => new { item.NotebookId, item.Path })
            .IsUnique();

        entity.HasIndex(item => new { item.NotebookId, item.ParentId, item.SortOrder });
        entity.HasIndex(item => new { item.NotebookId, item.IsArchived });
    }
}
