using MediatR;

namespace CodeCafe.Domain.Common;

// INotification lets the infrastructure interceptor publish events through MediatR as-is.
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAtUtc { get; }
}
