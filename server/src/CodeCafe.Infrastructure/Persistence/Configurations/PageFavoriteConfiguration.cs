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
