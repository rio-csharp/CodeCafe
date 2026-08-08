using MediatR;

namespace CodeCafe.Application.Common.Messaging;

public interface ICommand<TResponse> : IRequest<TResponse>;

public interface ICommand : IRequest;
