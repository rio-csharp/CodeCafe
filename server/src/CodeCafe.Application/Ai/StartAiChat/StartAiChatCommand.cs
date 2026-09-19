using MediatR;

namespace CodeCafe.Application.Ai.StartAiChat;

public sealed record StartAiChatCommand(string NotebookIdOrSlug, IReadOnlyList<AiChatMessage> Messages) : IStreamRequest<AiChatEvent>;
