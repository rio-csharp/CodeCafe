using MediatR;

namespace CodeCafe.Application.Common.Messaging;

public interface IQuery<TResponse> : IRequest<TResponse>;
