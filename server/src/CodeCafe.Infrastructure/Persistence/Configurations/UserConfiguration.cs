using CodeCafe.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email).HasMaxLength(User.MaxEmailLength).IsRequired();

        builder
            .Property(user => user.NormalizedEmail)
            .HasMaxLength(User.MaxEmailLength)
            .IsRequired();

        // Case-insensitive uniqueness lives in the application layer (lowercased values), so a
        // plain unique index is enough here.
        builder.HasIndex(user => user.NormalizedEmail).IsUnique();

        builder.Property(user => user.DisplayName).HasMaxLength(User.MaxDisplayNameLength).IsRequired();

        builder.Property(user => user.PasswordHash).IsRequired();

        // PostgreSQL xmin optimistic concurrency (decision 10) via the system column as a
        // shadow property; Npgsql 10 dropped UseXminAsConcurrencyToken(), and xmin is
        // implicitly present on every table so no column is emitted. Users are rarely
        // contended, but the pattern stays uniform across entities.
        builder
            .Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
