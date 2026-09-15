using CodeCafe.Application.Ai.Commands;
using CodeCafe.Application.Ai.Models;
using MediatR;
namespace CodeCafe.Application.Ai.Handlers;

public sealed class StartAiChatCommandHandler : IStreamRequestHandler<StartAiChatCommand, AiChatEvent>
{
    public IAsyncEnumerable<AiChatEvent> Handle(StartAiChatCommand request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
