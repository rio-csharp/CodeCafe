using MediatR;

namespace CodeCafe.Application.Common.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<TResponse>;
