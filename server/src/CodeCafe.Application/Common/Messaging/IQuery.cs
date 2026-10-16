using MediatR;

namespace CodeCafe.Application.Common.Messaging;

public interface IQuery<out TResult> : IRequest<TResult>;
