using OpenAI;
using System.ClientModel;

namespace CodeCafe.Modules.Ai.Configuration;

public static class OpenAiClientFactory
{
    public static OpenAIClient Create(AiOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return new OpenAIClient(options.ApiKey);
        }

        return new OpenAIClient(
            new ApiKeyCredential(options.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = NormalizeEndpoint(options.BaseUrl)
            });
    }

    public static Uri NormalizeEndpoint(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var endpoint)
            || endpoint.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("Ai:BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        var builder = new UriBuilder(endpoint);
        var path = builder.Path.TrimEnd('/');
        builder.Path = string.IsNullOrWhiteSpace(path) || path == "/"
            ? "/v1"
            : path;
        return builder.Uri;
    }
}
