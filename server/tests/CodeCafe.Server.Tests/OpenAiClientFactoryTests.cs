using CodeCafe.Infrastructure.Ai.Agents;
using CodeCafe.Infrastructure.Ai;
using CodeCafe.Application.Ai;
using Xunit;

namespace CodeCafe.Server.Tests;

public sealed class OpenAiClientFactoryTests
{
    [Theory]
    [InlineData("https://www.tokenrouter.tech/", "https://www.tokenrouter.tech/v1")]
    [InlineData("https://www.tokenrouter.tech", "https://www.tokenrouter.tech/v1")]
    [InlineData("https://www.tokenrouter.tech/v1/", "https://www.tokenrouter.tech/v1")]
    [InlineData("https://router.example.test/openai", "https://router.example.test/openai")]
    public void NormalizeEndpoint_NormalizesProviderRootToOpenAiV1Endpoint(
        string baseUrl,
        string expectedEndpoint)
    {
        var endpoint = OpenAiClientFactory.NormalizeEndpoint(baseUrl);

        Assert.Equal(expectedEndpoint, endpoint.ToString().TrimEnd('/'));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://router.example.test")]
    public void NormalizeEndpoint_RejectsInvalidEndpoint(string baseUrl)
    {
        Assert.Throws<InvalidOperationException>(() => OpenAiClientFactory.NormalizeEndpoint(baseUrl));
    }
}
