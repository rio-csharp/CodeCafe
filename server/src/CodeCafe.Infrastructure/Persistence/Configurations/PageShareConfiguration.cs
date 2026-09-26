using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class PageShareConfiguration : IEntityTypeConfiguration<PageShare>
{
    public void Configure(EntityTypeBuilder<PageShare> builder)
    {
        builder.ToTable("page_shares");

        builder.HasKey(share => share.Id);

        builder.Property(share => share.Role).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(share => new { share.PageId, share.UserId }).IsUnique();

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(share => share.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
