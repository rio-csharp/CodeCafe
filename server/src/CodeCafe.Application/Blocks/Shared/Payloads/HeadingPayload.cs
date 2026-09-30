using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.Shared.Payloads;

public sealed record HeadingPayload
{
    public required int Level { get; init; }

    public required Spans Spans { get; init; }
}
