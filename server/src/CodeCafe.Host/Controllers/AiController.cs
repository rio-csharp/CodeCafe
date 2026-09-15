using CodeCafe.Application.Ai.Commands;
using CodeCafe.Application.Ai.Models;
using CodeCafe.Application.Common;
using CodeCafe.Host.Hosting;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCafe.Host.Controllers;

[ApiController]
[Route("api")]
[Tags("AI")]
public sealed class AiController(ISender sender) : ControllerBase
{
    // Streamed as Server-Sent Events: one SSE event per AiChatEvent, with the SSE
    // `event` field carrying AiChatEvent.Kind and `data` carrying the JSON payload.
    [HttpPost("notebooks/{idOrSlug}/ai/chat")]
    [EnableRateLimiting(RateLimiterExtensions.AiPolicy)]
    [RequestSizeLimit(1024 * 1024)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/event-stream")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status429TooManyRequests)]
    public IResult Chat(string idOrSlug, AiChatRequest request)
        => new SseResult<AiChatEvent>(
            sender.CreateStream(new StartAiChatCommand(idOrSlug, request.Messages)),
            aiEvent => aiEvent.Kind);
}
