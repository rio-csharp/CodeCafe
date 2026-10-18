using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class PageFavoriteConfiguration : IEntityTypeConfiguration<PageFavorite>
{
    public void Configure(EntityTypeBuilder<PageFavorite> builder)
    {
        builder.ToTable("page_favorites");

        builder.HasKey(favorite => new { favorite.PageId, favorite.UserId });

        // The user's favorites read filters by UserId alone; the primary key starts at PageId.
        builder.HasIndex(favorite => new { favorite.UserId, favorite.PageId });

        builder
            .HasOne<Page>()
            .WithMany()
            .HasForeignKey(favorite => favorite.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(favorite => favorite.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
