using CodeCafe.Modules.Identity.Infrastructure.Identity;
using CodeCafe.Domain.Notes;
using CodeCafe.Shared.Application.Common.Interfaces;
using CodeCafe.Domain.Ai;
using CodeCafe.Domain.Common;
using CodeCafe.Domain.Mcp;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Shared.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDateTimeProvider dateTimeProvider)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public DbSet<Notebook> Notebooks { get; set; }

    public DbSet<NotebookItem> NotebookItems { get; set; }

    public DbSet<NotebookFavorite> NotebookFavorites { get; set; }

    public DbSet<McpToolAuditEntry> McpToolAuditEntries { get; set; }

    public DbSet<McpUploadSessionEntry> McpUploadSessions { get; set; }

    public DbSet<McpUploadChunkEntry> McpUploadChunks { get; set; }

    public DbSet<AiEditProposal> AiEditProposals { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Required for the trigram GIN indexes on notebook/item search columns.
        builder.HasPostgresExtension("pg_trgm");

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetAuditFields()
    {
        var now = dateTimeProvider.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;

                if (entry.Entity is NotebookItem notebookItem && notebookItem.Revision <= 0)
                {
                    notebookItem.Revision = 1;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAtUtc).IsModified = false;
                entry.Entity.UpdatedAtUtc = now;

                if (entry.Entity is NotebookItem notebookItem)
                {
                    notebookItem.Revision += 1;
                }
            }
        }
    }
}
