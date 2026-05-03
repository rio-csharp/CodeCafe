namespace CodeCafe.Contracts.Ai;

public sealed record AiProviderResponse(
    Guid Id,
    string Name,
    string BaseUrl,
    string? ApiKey,
    bool Enabled,
    bool BuiltIn,
    IReadOnlyCollection<AiProviderModelResponse> Models);

public sealed record AiProviderModelResponse(
    Guid Id,
    string ModelId,
    string DisplayName,
    bool Enabled,
    string Kind);

public sealed record UpsertAiProviderRequest(
    string Name,
    string BaseUrl,
    string? ApiKey,
    bool Enabled);

public sealed record UpsertAiProviderModelRequest(
    string ModelId,
    string DisplayName,
    bool Enabled,
    string Kind);
