namespace CodeCafe.Ai.Configuration;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public bool Enabled { get; set; }

    public string EndpointPath { get; set; } = "/api/ai/assistant";

    public string StatusEndpointPath { get; set; } = "/api/ai/status";

    public string DraftEndpointPath { get; set; } = "/api/ai/drafts";

    public string AgentName { get; set; } = "CodeCafeAssistant";

    public string Model { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public int MaxToolResults { get; set; } = 10;

    public int MaxToolContentChars { get; set; } = 4000;

    public int MaxDraftPromptChars { get; set; } = 2000;

    public int MaxDraftContextChars { get; set; } = 12000;

    public int MaxDraftOutputTokens { get; set; } = 1600;
}
