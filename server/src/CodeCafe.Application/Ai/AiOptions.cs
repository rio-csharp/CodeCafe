namespace CodeCafe.Application.Ai;

public enum AiWireFormat
{
    ChatCompletions,
    Responses,
}

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public bool Enabled { get; set; }

    public string EndpointPath { get; set; } = "/api/ai/assistant";

    public string StatusEndpointPath { get; set; } = "/api/ai/status";

    public string EditEndpointPath { get; set; } = "/api/ai/edits";

    public string DraftEndpointPath { get; set; } = "/api/ai/drafts";

    public string AgentName { get; set; } = "CodeCafeAssistant";

    public string Model { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public AiWireFormat WireFormat { get; set; } = AiWireFormat.ChatCompletions;

    public int MaxToolResults { get; set; } = 10;

    public int MaxToolContentChars { get; set; } = 4000;

    public int MaxDraftPromptChars { get; set; } = 2000;

    public int MaxDraftContextChars { get; set; } = 200000;

    public int MaxDraftOutputTokens { get; set; } = 1600;

    public int NetworkTimeoutSeconds { get; set; } = 100;

    public int MaxChatOutputTokens { get; set; } = 1600;

    public int MaxChatHistoryMessages { get; set; } = 40;

    public int MaxChatHistoryChars { get; set; } = 100000;

    public int MaxAgUiContextEntries { get; set; } = 8;

    public int EditProposalTtlMinutes { get; set; } = 30;
}
