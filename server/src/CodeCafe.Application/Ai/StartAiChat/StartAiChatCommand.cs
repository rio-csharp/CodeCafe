using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Ai.StartAiChat;

public sealed record StartAiChatCommand(string NotebookIdOrSlug, IReadOnlyList<AiChatMessage> Messages) : IStreamCommand<AiChatEvent>;
