using System.ComponentModel;
using CodeCafe.Application.Ai.StartAiChat;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeCafe.Host.Mcp.Tools;

[McpServerToolType]
public sealed class AiMcpTools
{
    // MCP tools cannot stream, so the chat is collected and returned as the full event list.
    [McpServerTool(Name = "codecafe_ai_chat")]
    [Description("Chat with a notebook's AI assistant. Returns the chat's full event list once it completes.")]
    public static async Task<CallToolResult> AiChat(
        ISender sender,
        string idOrSlug,
        IReadOnlyList<AiChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var events = new List<AiChatEvent>();
        await foreach (var aiEvent in sender
            .CreateStream(new StartAiChatCommand(idOrSlug, messages))
            .WithCancellation(cancellationToken))
        {
            events.Add(aiEvent);
        }

        return McpToolResults.Success(events);
    }
}
