using MediatR;

namespace CodeCafe.Application.Common.Messaging;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
