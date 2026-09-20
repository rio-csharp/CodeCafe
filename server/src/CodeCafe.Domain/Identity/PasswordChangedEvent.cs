using CodeCafe.Domain.Primitives;

namespace CodeCafe.Domain.Identity;

public sealed record PasswordChangedEvent(Guid UserId) : IDomainEvent;
