using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.Shared.Payloads;

public sealed record ParagraphPayload
{
    public required Spans Spans { get; init; }
}
