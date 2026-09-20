using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher)
    : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
        => await SaveChangesAsync(cancellationToken);

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

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
