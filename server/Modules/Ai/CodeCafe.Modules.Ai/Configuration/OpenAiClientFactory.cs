using OpenAI;
using System.ClientModel;

namespace CodeCafe.Modules.Ai.Configuration;

public static class OpenAiClientFactory
{
    public static OpenAIClient Create(AiOptions options)
    {
        // Always set an explicit network timeout: without it a hung upstream surfaces as an
        // unobserved TaskCanceledException instead of a timeout the handlers can map to 504.
        var clientOptions = new OpenAIClientOptions
        {
            NetworkTimeout = TimeSpan.FromSeconds(Math.Max(1, options.NetworkTimeoutSeconds))
        };

        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            clientOptions.Endpoint = NormalizeEndpoint(options.BaseUrl);
        }

        return new OpenAIClient(new ApiKeyCredential(options.ApiKey), clientOptions);
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
