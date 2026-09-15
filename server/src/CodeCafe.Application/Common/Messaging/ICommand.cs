using MediatR;

namespace CodeCafe.Application.Common.Messaging;

public interface ICommand<out TResult> : IRequest<TResult>;
