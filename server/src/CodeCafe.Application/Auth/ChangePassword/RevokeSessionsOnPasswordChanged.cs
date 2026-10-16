using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Domain.Identity;
using MediatR;

namespace CodeCafe.Application.Auth.ChangePassword;

// Subscribing to the domain event keeps the use-case handler free of session concerns;
// new reactions to a password change become new subscribers instead of edits here.
public sealed class RevokeSessionsOnPasswordChanged(IRefreshTokenService refreshTokens)
    : INotificationHandler<PasswordChangedEvent>
{
    public Task Handle(PasswordChangedEvent notification, CancellationToken cancellationToken)
        => refreshTokens.RevokeAllForUserAsync(notification.UserId, cancellationToken);
}
