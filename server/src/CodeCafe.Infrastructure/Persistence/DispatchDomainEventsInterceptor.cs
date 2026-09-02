using CodeCafe.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CodeCafe.Infrastructure.Persistence;

// Events describe facts that already happened, so they are published only after the commit succeeds.
public sealed class DispatchDomainEventsInterceptor(IPublisher mediator) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is not null)
        {
            var entries = eventData.Context.ChangeTracker
                .Entries<Entity>()
                .Where(entry => entry.Entity.DomainEvents.Count != 0)
                .ToArray();
            var events = entries.SelectMany(entry => entry.Entity.DomainEvents).ToArray();

            foreach (var entry in entries)
            {
                entry.Entity.ClearDomainEvents();
            }

            foreach (var domainEvent in events)
            {
                await mediator.Publish(domainEvent, cancellationToken);
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
