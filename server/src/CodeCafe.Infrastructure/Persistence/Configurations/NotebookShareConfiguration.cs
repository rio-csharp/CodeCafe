using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class NotebookShareConfiguration : IEntityTypeConfiguration<NotebookShare>
{
    public void Configure(EntityTypeBuilder<NotebookShare> builder)
    {
        builder.ToTable("notebook_shares");

        builder.HasKey(share => share.Id);

        builder.Property(share => share.Role).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(share => new { share.NotebookId, share.UserId }).IsUnique();

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(share => share.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
