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

        // PostgreSQL's xmin system column as the concurrency token. Npgsql 10 dropped
        // UseXminAsConcurrencyToken(), and xmin already exists on every table, so mapping it as a
        // shadow property emits no column.
        builder
            .Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
