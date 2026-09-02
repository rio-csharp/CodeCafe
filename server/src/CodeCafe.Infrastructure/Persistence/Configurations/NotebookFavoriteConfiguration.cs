using CodeCafe.Domain.Notes;
using CodeCafe.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class NotebookFavoriteConfiguration : IEntityTypeConfiguration<NotebookFavorite>
{
    public void Configure(EntityTypeBuilder<NotebookFavorite> entity)
    {
        entity.ToTable("NotebookFavorites");

        entity.HasKey(favorite => favorite.Id);

        entity.Property(x => x.Id).ValueGeneratedNever();

        entity.Ignore(favorite => favorite.DomainEvents);

        entity.Property(favorite => favorite.CreatedAtUtc).IsRequired();

        entity.Property(favorite => favorite.UpdatedAtUtc);

        entity
            .HasOne<Notebook>()
            .WithMany()
            .HasForeignKey(favorite => favorite.NotebookId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(favorite => favorite.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(favorite => new { favorite.NotebookId, favorite.UserId }).IsUnique();

        entity.HasIndex(favorite => favorite.UserId);
    }
}
