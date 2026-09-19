using MediatR;

namespace CodeCafe.Domain.Primitives;

// Marker for aggregate-raised events; extending MediatR's notification contract lets the
// persistence layer dispatch them through the same pipeline as application notifications.
public interface IDomainEvent : INotification;
