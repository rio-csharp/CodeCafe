using CodeCafe.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class PersonalAccessTokenConfiguration : IEntityTypeConfiguration<PersonalAccessToken>
{
    public void Configure(EntityTypeBuilder<PersonalAccessToken> builder)
    {
        builder.ToTable("personal_access_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Name).HasMaxLength(PersonalAccessToken.MaxNameLength).IsRequired();

        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();

        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
