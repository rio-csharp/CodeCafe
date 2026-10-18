using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Domain.Revisions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Application.Ai.StartAiChat;

// One DI scope per tool call. The AI turn is a single HTTP request, so every tool would otherwise
// share one scoped DbContext: a failed or dry-run batch leaves tracked mutations behind, and the
// next tool's SaveChanges would flush them. A fresh scope also gives each tool its own transaction
// and its own change-source accessor, which is set back to Ai for revision attribution.
internal sealed class ScopedSender(IServiceScopeFactory scopeFactory) : ISender
{
    public async Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = BeginScope(cancellationToken);
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request, cancellationToken);
    }

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        await using var scope = BeginScope(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(request, cancellationToken);
    }

    public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        await using var scope = BeginScope(cancellationToken);
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request, cancellationToken);
    }

    // Assistant tools are all request/response; a stream tool would need a scope that outlives the
    // call, so it fails loudly rather than leaking an undisposed context.
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException("Assistant tools never create streams.");

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Assistant tools never create streams.");

    private AsyncServiceScope BeginScope(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IChangeSourceAccessor>().Source = RevisionSource.Ai;
        return scope;
    }
}
