namespace CodeCafe.Domain.Ai;

using CodeCafe.Domain.Common;

public sealed class AiProviderModel : Entity
{
    public AiProviderModel(
        string modelId,
        string displayName,
        bool enabled,
        AiProviderModelKind kind)
    {
        ModelId = modelId;
        DisplayName = displayName;
        Enabled = enabled;
        Kind = kind;
    }

    public string ModelId { get; private set; }

    public string DisplayName { get; private set; }

    public bool Enabled { get; private set; }

    public AiProviderModelKind Kind { get; private set; }

    public void Update(string modelId, string displayName, bool enabled, AiProviderModelKind kind)
    {
        ModelId = modelId;
        DisplayName = displayName;
        Enabled = enabled;
        Kind = kind;
    }
}
