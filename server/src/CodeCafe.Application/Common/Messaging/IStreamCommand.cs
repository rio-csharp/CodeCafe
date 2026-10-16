using MediatR;

namespace CodeCafe.Application.Common.Messaging;

public interface IStreamCommand<out TResponse> : IStreamRequest<TResponse>;
