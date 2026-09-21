using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class NotebookFavoriteConfiguration : IEntityTypeConfiguration<NotebookFavorite>
{
    public void Configure(EntityTypeBuilder<NotebookFavorite> builder)
    {
        builder.ToTable("notebook_favorites");

        builder.HasKey(favorite => new { favorite.NotebookId, favorite.UserId });

        builder
            .HasOne<Notebook>()
            .WithMany()
            .HasForeignKey(favorite => favorite.NotebookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(favorite => favorite.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
