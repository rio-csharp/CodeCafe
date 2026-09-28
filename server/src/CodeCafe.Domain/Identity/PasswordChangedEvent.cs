using CodeCafe.Domain.Primitives;

namespace CodeCafe.Domain.Identity;

// Subscribers: RevokeSessionsOnPasswordChanged (Application/Auth/ChangePassword). Registration is
// by assembly scan, so nothing else in the codebase lists them; this line and
// DomainEventSubscriberTests are the only places that do.
public sealed record PasswordChangedEvent(Guid UserId) : IDomainEvent;
