using MediatR;

namespace CodeCafe.Application.Common.Abstractions.Messaging;

public interface ICommand<TResponse> : IRequest<TResponse>;

public interface ICommand : IRequest;
