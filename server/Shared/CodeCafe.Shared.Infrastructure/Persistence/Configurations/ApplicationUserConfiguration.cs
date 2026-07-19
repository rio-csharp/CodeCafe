using CodeCafe.Modules.Identity.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Shared.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> entity)
    {
        entity.Property(user => user.DisplayName)
            .HasMaxLength(40)
            .IsRequired();

        entity.Property(user => user.CreatedAtUtc)
            .IsRequired();

        entity.Property(user => user.UpdatedAtUtc);

        entity.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();
    }
}
