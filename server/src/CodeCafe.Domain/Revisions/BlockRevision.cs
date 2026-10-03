using CodeCafe.Domain.Primitives;

namespace CodeCafe.Domain.Revisions;

// One lifecycle event for one block: every insert/update/move/delete writes a row, so the log
// is a complete per-page history and the state at any instant is reconstructible as "each
// block's latest event at or before that instant". Rows outlive their block (blocks are
// hard-deleted), which is exactly what lets a restore re-create a block with its original id.
//
// The log grows without bound (one small row per block edit). If history volume ever hurts,
// the two levers, in ascending order of complexity:
// 1. Retention: prune rows older than a configured window / beyond N versions per block.
// 2. Compaction: squash old batches into a baseline snapshot per page, so restores start from
//    "baseline + deltas" instead of from the page's creation.
public sealed class BlockRevision : Entity
{
    private BlockRevision()
        : base(Guid.Empty) { }

    private BlockRevision(
        Guid id,
        Guid pageId,
        Guid blockId,
        Guid batchId,
        long blockVersion,
        BlockChangeKind changeKind,
        RevisionSource source,
        Guid? parentBlockId,
        string sortKey,
        string type,
        string contentJson,
        string plainText,
        string? subtreeJson
    )
        : base(id)
    {
        PageId = pageId;
        BlockId = blockId;
        BatchId = batchId;
        BlockVersion = blockVersion;
        ChangeKind = changeKind;
        Source = source;
        ParentBlockId = parentBlockId;
        SortKey = sortKey;
        Type = type;
        ContentJson = contentJson;
        PlainText = plainText;
        SubtreeJson = subtreeJson;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid PageId { get; private set; }

    // NOT a foreign key: the block may be long deleted by design.
    public Guid BlockId { get; private set; }

    // All rows written by one handler transaction share a batch, so a multi-op change (an
    // ApplyBlockOps run, an import, a restore) reads as one group in the page history.
    public Guid BatchId { get; private set; }

    // The block's own version counter after this change. Recreated blocks restart at 1, so the
    // pair (BlockId, BlockVersion) is not unique across eras — lookups always take the newest row.
    public long BlockVersion { get; private set; }

    public BlockChangeKind ChangeKind { get; private set; }

    public RevisionSource Source { get; private set; }

    // Structural snapshot: the parent and sibling position the block had right after this
    // change. Page restore rebuilds the tree from these two columns.
    public Guid? ParentBlockId { get; private set; }

    public string SortKey { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    // Canonical payload snapshot, stored exactly as the block carried it (already normalized).
    public string ContentJson { get; private set; } = string.Empty;

    public string PlainText { get; private set; } = string.Empty;

    // Only set on Deleted rows of a subtree ROOT: the whole subtree as nested JSON
    // ({ id, type, content, plainText, children: [...] }), so a single-block restore can bring
    // back everything that was deleted with it. Other nodes of the subtree have their own
    // Deleted rows (page restore reads those) but no snapshot.
    public string? SubtreeJson { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static BlockRevision Record(
        Guid pageId,
        Guid blockId,
        Guid batchId,
        long blockVersion,
        BlockChangeKind changeKind,
        RevisionSource source,
        Guid? parentBlockId,
        string sortKey,
        string type,
        string contentJson,
        string plainText,
        string? subtreeJson = null,
        DateTimeOffset? atUtc = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(contentJson);
        ArgumentNullException.ThrowIfNull(plainText);

        var row = new BlockRevision(
            Guid.CreateVersion7(),
            pageId,
            blockId,
            batchId,
            blockVersion,
            changeKind,
            source,
            parentBlockId,
            sortKey,
            type,
            contentJson,
            plainText,
            subtreeJson
        );
        // Optional explicit timestamp: restores and tests reconstruct exact points in time.
        if (atUtc is not null)
        {
            row.CreatedAtUtc = atUtc.Value;
        }

        return row;
    }
}
