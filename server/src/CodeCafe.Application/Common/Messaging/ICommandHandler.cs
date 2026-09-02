using MediatR;

namespace CodeCafe.Application.Common.Messaging;

public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
