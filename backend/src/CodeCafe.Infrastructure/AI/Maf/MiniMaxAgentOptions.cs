namespace CodeCafe.Infrastructure.AI.Maf;

public sealed class MiniMaxAgentOptions
{
    public const string SectionName = "AI:MiniMax";
    public const string HttpClientName = "MiniMax";
    public const string DefaultBaseUrl = "https://api.minimaxi.com/v1";
    public const string DefaultModel = "MiniMax-M2.7";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public string Model { get; set; } = DefaultModel;

    public string Provider { get; set; } = "Auto";
}
