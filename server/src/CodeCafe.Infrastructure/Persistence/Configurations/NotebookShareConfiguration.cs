using CodeCafe.Domain.Notes;
using CodeCafe.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class NotebookShareConfiguration : IEntityTypeConfiguration<NotebookShare>
{
    public void Configure(EntityTypeBuilder<NotebookShare> entity)
    {
        entity.ToTable("NotebookShares");

        entity.HasKey(share => share.Id);

        entity.Property(x => x.Id).ValueGeneratedNever();

        entity.Ignore(share => share.DomainEvents);

        entity.Property(share => share.CreatedAtUtc).IsRequired();

        entity.Property(share => share.UpdatedAtUtc);

        entity
            .HasOne<Notebook>()
            .WithMany()
            .HasForeignKey(share => share.NotebookId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(share => share.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(share => share.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(share => new { share.NotebookId, share.UserId }).IsUnique();

        entity.HasIndex(share => share.UserId);
    }
}
