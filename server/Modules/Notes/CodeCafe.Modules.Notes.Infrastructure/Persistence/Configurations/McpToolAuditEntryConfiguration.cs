using CodeCafe.Domain.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Infrastructure.Persistence.Configurations;

public sealed class McpToolAuditEntryConfiguration : IEntityTypeConfiguration<McpToolAuditEntry>
{
    public void Configure(EntityTypeBuilder<McpToolAuditEntry> entity)
    {
        entity.ToTable("McpToolAuditEntries");

        entity.HasKey(entry => entry.Id);

        entity.Property(entry => entry.ActorType)
            .HasMaxLength(40)
            .IsRequired();

        entity.Property(entry => entry.ToolName)
            .HasMaxLength(160)
            .IsRequired();

        entity.Property(entry => entry.ResultCode)
            .HasMaxLength(80)
            .IsRequired();

        entity.Property(entry => entry.ErrorCode)
            .HasMaxLength(80);

        entity.Property(entry => entry.CreatedAtUtc)
            .IsRequired();

        entity.Property(entry => entry.UpdatedAtUtc);

        entity.HasIndex(entry => new { entry.ToolName, entry.CreatedAtUtc });
        entity.HasIndex(entry => new { entry.ActorUserId, entry.CreatedAtUtc });
    }
}
