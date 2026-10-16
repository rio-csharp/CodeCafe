using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.Shared.Payloads;

public sealed record CalloutPayload
{
    public required PaletteColor Variant { get; init; }

    public required Spans Spans { get; init; }
}
