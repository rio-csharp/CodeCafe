using MediatR;

namespace CodeCafe.Application.Tests.Ai;

// Routes requests to canned responses by runtime type; anything unrouted is a test bug.
internal sealed class StubSender : ISender
{
    private readonly Dictionary<Type, object> _responses = new();

    public List<object> Received { get; } = [];

    public StubSender Respond<TRequest>(object response)
    {
        _responses[typeof(TRequest)] = response;
        return this;
    }

    public T? LastOfType<T>() => Received.OfType<T>().LastOrDefault();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        Received.Add(request);
        return _responses.TryGetValue(request.GetType(), out var response)
            ? Task.FromResult((TResponse)response)
            : throw new InvalidOperationException($"No canned response for {request.GetType().Name}.");
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        Received.Add(request!);
        return _responses.ContainsKey(request!.GetType())
            ? Task.CompletedTask
            : throw new InvalidOperationException($"No canned response for {request.GetType().Name}.");
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        Received.Add(request);
        return _responses.TryGetValue(request.GetType(), out var response)
            ? Task.FromResult<object?>(response)
            : throw new InvalidOperationException($"No canned response for {request.GetType().Name}.");
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;

    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
