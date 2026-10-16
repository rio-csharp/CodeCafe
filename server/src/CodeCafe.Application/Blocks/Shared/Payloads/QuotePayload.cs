using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.Shared.Payloads;

public sealed record QuotePayload
{
    public required Spans Spans { get; init; }
}
