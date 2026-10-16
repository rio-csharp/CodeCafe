using MediatR;

namespace CodeCafe.Infrastructure.Persistence;

// For design-time tooling and tests that build the context without MediatR.
public sealed class NullPublisher : IPublisher
{
    public static readonly NullPublisher Instance = new();

    private NullPublisher() { }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => Task.CompletedTask;
}
