using CodeCafe.Domain.Primitives;

namespace CodeCafe.Domain.Blocks;

public sealed class Block : Entity
{
    // LexoRank-style keys grow one level per tight insertion; 64 chars is far beyond practice.
    public const int MaxSortKeyLength = 64;

    private Block()
        : base(Guid.Empty) { }

    private Block(Guid id, Guid pageId, Guid? parentBlockId, string type, string contentJson, string plainText, string sortKey)
        : base(id)
    {
        PageId = pageId;
        ParentBlockId = parentBlockId;
        Type = type;
        ContentJson = contentJson;
        PlainText = plainText;
        SortKey = sortKey;
        Version = 1;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid PageId { get; private set; }

    // Null at the top level: the block then hangs off Page.FirstBlockId's chain.
    public Guid? ParentBlockId { get; private set; }

    // Lowercase type name; the Application-layer registry is the validation authority.
    public string Type { get; private set; } = string.Empty;

    // Raw canonical payload; the entity never parses it, typed access goes through the registry.
    public string ContentJson { get; private set; } = string.Empty;

    // Precomputed search projection handed in by the caller, regenerated on every write.
    public string PlainText { get; private set; } = string.Empty;

    // Dual ordering, mirroring pages: the sibling chains are the structural truth that writes
    // maintain, while SortKey is their indexed projection that reads order by.
    public string SortKey { get; private set; } = string.Empty;

    public Guid? FirstChildId { get; private set; }

    public Guid? NextSiblingId { get; private set; }

    // Optimistic concurrency token for payload updates; moves bump it too, so a client holding a
    // stale structure cannot silently overwrite a payload.
    public long Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Block Create(Guid pageId, Guid? parentBlockId, string type, string contentJson, string plainText, string sortKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(contentJson);
        ArgumentNullException.ThrowIfNull(plainText);
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);

        return new Block(Guid.CreateVersion7(), pageId, parentBlockId, type, contentJson, plainText, sortKey);
    }

    // Re-creates a hard-deleted block with its ORIGINAL id, so AI sessions and links referencing
    // the id keep working after a revision restore. The version counter restarts at 1; the
    // revision log resolves collisions by always taking the newest row with a given number.
    public static Block Restore(Guid id, Guid pageId, Guid? parentBlockId, string type, string contentJson, string plainText, string sortKey)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(contentJson);
        ArgumentNullException.ThrowIfNull(plainText);
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);

        return new Block(id, pageId, parentBlockId, type, contentJson, plainText, sortKey);
    }

    public void UpdateContent(string contentJson, string plainText)
    {
        ArgumentNullException.ThrowIfNull(contentJson);
        ArgumentNullException.ThrowIfNull(plainText);

        ContentJson = contentJson;
        PlainText = plainText;
        Version++;
        Touch();
    }

    // A move repositions the block without touching its payload; descendants keep their Version.
    public void MarkMoved()
    {
        Version++;
        Touch();
    }

    // Chain bookkeeping below is BlockChain-only, so handlers can never corrupt the structure.
    // Pointer writes do not bump Version or UpdatedAtUtc: neighbours are not being edited.
    internal void SetNextSibling(Guid? nextSiblingId) => NextSiblingId = nextSiblingId;

    internal void SetFirstChild(Guid? firstChildId) => FirstChildId = firstChildId;

    // Reposition under a (possibly) new parent; FirstChildId is deliberately untouched: the
    // child chain moves with the block. Version/UpdatedAtUtc are MarkMoved's job.
    internal void MoveTo(Guid? parentBlockId, string sortKey, Guid? nextSiblingId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);

        ParentBlockId = parentBlockId;
        SortKey = sortKey;
        NextSiblingId = nextSiblingId;
    }

    // Rebalance bookkeeping only: re-keying a level must not mark untouched blocks as updated.
    internal void Rekey(string sortKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);

        SortKey = sortKey;
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
