using CodeCafe.Domain.Ai;
using CodeCafe.Domain.Common;
using CodeCafe.Domain.Mcp;
using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Uploads;
using CodeCafe.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    TimeProvider timeProvider
) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public DbSet<Notebook> Notebooks { get; set; }

    public DbSet<NotebookItem> NotebookItems { get; set; }

    public DbSet<NotebookFavorite> NotebookFavorites { get; set; }

    public DbSet<NotebookShare> NotebookShares { get; set; }

    public DbSet<McpToolAuditEntry> McpToolAuditEntries { get; set; }

    public DbSet<UploadSession> UploadSessions { get; set; }

    public DbSet<UploadChunk> UploadChunks { get; set; }

    public DbSet<AiEditProposal> AiEditProposals { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
        CancellationToken cancellationToken = default
    )
    {
        SetAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetAuditFields()
    {
        var now = timeProvider.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAtUtc).IsModified = false;
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
