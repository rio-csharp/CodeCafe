using CodeCafe.Application.Notes;

namespace CodeCafe.Application.Tests;

public sealed class ContentUrlPolicyTests
{
    [Theory]
    [InlineData("https://example.com/page?x=1#frag")]
    [InlineData("http://example.com")]
    [InlineData("HTTPS://EXAMPLE.COM")]
    [InlineData("mailto:user@example.com")]
    [InlineData("tel:+1234567890")]
    [InlineData("/docs/getting-started")]
    [InlineData("/")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsAllowedLinkUrl_AllowsInertUrls(string? url)
    {
        Assert.True(ContentUrlPolicy.IsAllowedLinkUrl(url));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JavaScript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("//evil.example/phish")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/file")]
    [InlineData("relative/path")]
    [InlineData("#fragment")]
    [InlineData("  javascript:alert(1)")]
    public void IsAllowedLinkUrl_DeniesActiveContentUrls(string url)
    {
        Assert.False(ContentUrlPolicy.IsAllowedLinkUrl(url));
    }

    [Theory]
    [InlineData("https://cdn.example.com/image.png")]
    [InlineData("http://example.com/a.gif")]
    [InlineData("/uploads/image.png")]
    [InlineData("")]
    [InlineData(null)]
    public void IsAllowedResourceUrl_AllowsInertUrls(string? url)
    {
        Assert.True(ContentUrlPolicy.IsAllowedResourceUrl(url));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz4=")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("//evil.example/tracker.png")]
    [InlineData("mailto:user@example.com")]
    [InlineData("tel:+1234567890")]
    public void IsAllowedResourceUrl_DeniesNonResourceUrls(string url)
    {
        Assert.False(ContentUrlPolicy.IsAllowedResourceUrl(url));
    }
}
