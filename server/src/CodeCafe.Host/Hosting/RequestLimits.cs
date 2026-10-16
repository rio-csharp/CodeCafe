namespace CodeCafe.Host.Hosting;

internal static class RequestLimits
{
    // A notebook export or a page file arrives whole, and the MCP import recipe advertises this
    // same number so the two cannot drift into a 413 the client was never warned about.
    // [RequestSizeLimit] needs a compile-time constant, hence the const.
    public const int ImportBytes = 5 * 1024 * 1024;
}
