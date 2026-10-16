namespace CodeCafe.Application.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public bool Enabled { get; set; }

    public string Model { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    // Null/empty targets the provider's default endpoint; relays set their base URL here.
    public string? BaseUrl { get; set; }

    public AiWireFormat WireFormat { get; set; } = AiWireFormat.OpenAiChatCompletions;

    // The relay in front of the model. Generic needs no compat work; named relays activate an
    // adapter bundling their known quirks at the transport boundary.
    public AiRelayKind Relay { get; set; } = AiRelayKind.Generic;

    public int MaxOutputTokens { get; set; } = 16_384;

    // Caps are in chars: token counts vary by provider, chars are the knob we honestly control.
    // Prompt order is system prompt, tools, notebook outline, then history — the stable prefix
    // comes first so provider-side prefix caching can hit (Anthropic's explicit cache_control
    // breakpoints are a future refinement on top of this ordering).
    public int MaxOutlineChars { get; set; } = 24_000;

    // Hard ceiling only. Trimming is the CLIENT's job: it owns the history, and server-side
    // trimming from the front would change the prompt prefix and defeat prefix caching.
    public int MaxHistoryChars { get; set; } = 400_000;

    public int MaxToolResultChars { get; set; } = 24_000;

    public int MaxToolIterations { get; set; } = 16;

    // Retries against flaky relays: rate limits (429), server errors (5xx), and 401s (relays
    // proxy upstream OAuth tokens that churn) get exponential backoff; other 4xxs indicate real
    // caller/config errors and fail fast. Streaming retries only before the first update — a
    // mid-stream failure cannot be replayed without duplicating already-emitted text.
    // 0 disables.
    public int MaxProviderRetries { get; set; } = 5;

    public int ProviderRetryBaseDelayMs { get; set; } = 1000;
}
