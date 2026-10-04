using MediatR;

namespace CodeCafe.Application.Common.Messaging;

public interface IStreamCommandHandler<in TCommand, TResponse> : IStreamRequestHandler<TCommand, TResponse>
    where TCommand : IStreamCommand<TResponse>;
