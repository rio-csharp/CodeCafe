using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.Shared.Payloads;

// One list entry. The list itself is implicit: consecutive same-type siblings form the list,
// and a numbered item's number is its position in that run — never stored.
public sealed record ListItemPayload
{
    public required Spans Spans { get; init; }
}
