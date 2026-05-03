namespace CodeCafe.Domain.Ai;

using CodeCafe.Domain.Common;

public sealed class AiProviderConfiguration : Entity
{
    private readonly List<AiProviderModel> models = [];

    public AiProviderConfiguration(
        string name,
        string baseUrl,
        string? apiKey,
        bool enabled,
        bool builtIn)
    {
        Name = name;
        BaseUrl = baseUrl;
        ApiKey = apiKey;
        Enabled = enabled;
        BuiltIn = builtIn;
    }

    public string Name { get; private set; }

    public string BaseUrl { get; private set; }

    public string? ApiKey { get; private set; }

    public bool Enabled { get; private set; }

    public bool BuiltIn { get; }

    public IReadOnlyCollection<AiProviderModel> Models => models;

    public void Update(string name, string baseUrl, string? apiKey, bool enabled)
    {
        Name = name;
        BaseUrl = baseUrl;
        ApiKey = apiKey;
        Enabled = enabled;
    }

    public AiProviderModel AddModel(AiProviderModel model)
    {
        models.Add(model);

        return model;
    }

    public bool RemoveModel(Guid modelId)
    {
        var model = models.SingleOrDefault(item => item.Id == modelId);

        return model is not null && models.Remove(model);
    }
}
