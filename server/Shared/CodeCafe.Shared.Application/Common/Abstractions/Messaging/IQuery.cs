using MediatR;

namespace CodeCafe.Shared.Application.Common.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<TResponse>;
