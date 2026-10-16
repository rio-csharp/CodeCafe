using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.Shared.Payloads;

public sealed record TodoPayload
{
    public required bool Checked { get; init; }

    public required Spans Spans { get; init; }
}
