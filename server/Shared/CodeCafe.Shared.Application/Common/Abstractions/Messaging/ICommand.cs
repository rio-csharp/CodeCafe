using MediatR;

namespace CodeCafe.Shared.Application.Common.Abstractions.Messaging;

public interface ICommand<TResponse> : IRequest<TResponse>;

public interface ICommand : IRequest;
