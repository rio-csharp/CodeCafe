using MediatR;

namespace CodeCafe.Application.Common.Messaging;

public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>;
