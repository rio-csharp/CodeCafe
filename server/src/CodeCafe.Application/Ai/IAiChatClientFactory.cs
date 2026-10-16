using Microsoft.Extensions.AI;

namespace CodeCafe.Application.Ai;

// The LLM endpoint is an external resource, so Application sees it through this seam.
public interface IAiChatClientFactory
{
    IChatClient Create();
}
