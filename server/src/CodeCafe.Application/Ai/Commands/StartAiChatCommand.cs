using CodeCafe.Application.Ai.Models;
using MediatR;

namespace CodeCafe.Application.Ai.Commands;

public sealed record StartAiChatCommand(string NotebookIdOrSlug, IReadOnlyList<AiChatMessage> Messages) : IStreamRequest<AiChatEvent>;
