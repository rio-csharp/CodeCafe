using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.Shared.Payloads;

// A rectangular grid: every row (and the header when present) has exactly Alignments.Count
// cells. Cells are single-line rich text, which is exactly what a Spans value guarantees;
// cells holding nested blocks are a future concern, not part of this contract.
public sealed record TablePayload
{
    // One entry per column; the count defines the table's width.
    public required IReadOnlyList<TableColumnAlignment> Alignments { get; init; }

    // Null when the table has no header row. Markdown pipe tables always have one, so imports
    // always set this; headerless tables exist for the editor's own use.
    public required IReadOnlyList<Spans>? Header { get; init; }

    public required IReadOnlyList<IReadOnlyList<Spans>> Rows { get; init; }
}
