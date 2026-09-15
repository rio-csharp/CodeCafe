using MediatR;

namespace CodeCafe.Application.Common.Messaging;

public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>;
