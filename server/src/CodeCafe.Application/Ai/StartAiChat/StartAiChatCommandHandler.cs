using MediatR;
namespace CodeCafe.Application.Ai.StartAiChat;

public sealed class StartAiChatCommandHandler : IStreamRequestHandler<StartAiChatCommand, AiChatEvent>
{
    public IAsyncEnumerable<AiChatEvent> Handle(StartAiChatCommand request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
