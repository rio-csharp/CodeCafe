using System.Net;
using CodeCafe.Host.Controllers;
using CodeCafe.Host.Hosting;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCafe.Host.Tests.Endpoints;

public sealed class AccessCodeRateLimitingTests(CodeCafeFactory factory)
    : IClassFixture<CodeCafeFactory>
{
    [Theory]
    [InlineData(typeof(NotebooksController), nameof(NotebooksController.Details))]
    [InlineData(typeof(NotebooksController), nameof(NotebooksController.Tree))]
    [InlineData(typeof(NotebooksController), nameof(NotebooksController.Export))]
    [InlineData(typeof(NotebooksController), nameof(NotebooksController.PageByPath))]
    [InlineData(typeof(PagesController), nameof(PagesController.Details))]
    [InlineData(typeof(PagesController), nameof(PagesController.Export))]
    public void AnonymousAccessCodeEndpoint_UsesTheAccessCodePolicy(Type controller, string action)
    {
        var method = Assert.Single(controller.GetMethods(), candidate => candidate.Name == action);
        var attribute = Assert.Single(method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true));

        Assert.Equal(RateLimiterExtensions.AccessCodePolicy, ((EnableRateLimitingAttribute)attribute).PolicyName);
    }

    [Fact]
    public async Task EleventhAccessCodeAttempt_IsRateLimited()
    {
        using var client = factory.CreateClient();
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            using var request = RequestWithAccessCode();
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        using var rejectedRequest = RequestWithAccessCode();
        using var rejected = await client.SendAsync(rejectedRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    private static HttpRequestMessage RequestWithAccessCode()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/notebooks/rate-limit-probe");
        request.Headers.Add(AccessCodeHeader.Name, "incorrect-code");
        return request;
    }
}
