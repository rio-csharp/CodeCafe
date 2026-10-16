using System.Text;

namespace CodeCafe.Host.Hosting;

internal static class FormFileExtensions
{
    // Markdown imports arrive as UTF-8 text. Reading the stream whole is bounded by the
    // [RequestSizeLimit] on the actions that call this.
    public static async Task<string> ReadMarkdownAsync(this IFormFile file, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            file.OpenReadStream(),
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true
        );
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
