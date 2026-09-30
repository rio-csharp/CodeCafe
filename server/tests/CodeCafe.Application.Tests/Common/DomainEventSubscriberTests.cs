using CodeCafe.Application;
using CodeCafe.Application.Auth.ChangePassword;
using CodeCafe.Domain.Identity;
using MediatR;

namespace CodeCafe.Application.Tests.Common;

// Handlers are registered by assembly scan, so nothing in the codebase enumerates the subscribers
// of a domain event. This test pins that list: a subscriber that stops being discoverable — moved
// out of the scanned assembly, no longer implementing INotificationHandler — would otherwise fail
// silently, and a password change would quietly stop revoking sessions.
public sealed class DomainEventSubscriberTests
{
    [Fact]
    public void PasswordChangedEvent_IsHandledByRevokingEverySession()
    {
        Assert.Equal(
            new[] { typeof(RevokeSessionsOnPasswordChanged) },
            SubscribersOf<PasswordChangedEvent>()
        );
    }

    // Anchored on DependencyInjection because that is the assembly AddCodeCafeApplication passes to
    // RegisterServicesFromAssembly, so this scans exactly what MediatR scans.
    private static IReadOnlyList<Type> SubscribersOf<TNotification>()
        where TNotification : INotification
        => typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type => type.IsAssignableTo(typeof(INotificationHandler<TNotification>)))
            .ToList();
}
