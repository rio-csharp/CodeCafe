using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Exceptions;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CodeCafe.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher)
    : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Notebook> Notebooks => Set<Notebook>();

    public DbSet<NotebookFavorite> NotebookFavorites => Set<NotebookFavorite>();

    public DbSet<Page> Pages => Set<Page>();

    public DbSet<PageFavorite> PageFavorites => Set<PageFavorite>();

    public DbSet<Block> Blocks => Set<Block>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
        => await SaveChangesAsync(cancellationToken);

    async Task<ITransaction> IUnitOfWork.BeginTransactionAsync(CancellationToken cancellationToken)
        => new EfTransaction(await Database.BeginTransactionAsync(cancellationToken));

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker
            .Entries<Entity>()
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToList();

        if (domainEvents.Count > 0)
        {
            foreach (var entry in ChangeTracker.Entries<Entity>())
            {
                entry.Entity.ClearDomainEvents();
            }

            // Handlers stage their reactions in this same context, so the change that raised
            // an event and every reaction to it commit in one transaction.
            foreach (var domainEvent in domainEvents)
            {
                await publisher.Publish(domainEvent, cancellationToken);
            }
        }

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            } violation)
        {
            throw new UniqueConstraintViolationException(violation.ConstraintName ?? string.Empty);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Backs the trigram indexes that keep notebook search a substring match instead of a scan.
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
