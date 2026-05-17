using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDateTimeProvider dateTimeProvider)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(user => user.CreatedAtUtc)
                .IsRequired();

            entity.Property(user => user.UpdatedAtUtc);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var now = dateTimeProvider.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
