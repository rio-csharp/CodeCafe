using CodeCafe.Domain.Primitives;

namespace CodeCafe.Domain.Blocks;

public sealed class Block : Entity
{
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

    public Guid? ParentBlockId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string ContentJson { get; private set; } = string.Empty;

    public string PlainText { get; private set; } = string.Empty;

    public string SortKey { get; private set; } = string.Empty;

    public Guid? FirstChildId { get; private set; }

    public Guid? NextSiblingId { get; private set; }

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

    public void MarkMoved()
    {
        Version++;
        Touch();
    }

    internal void SetNextSibling(Guid? nextSiblingId) => NextSiblingId = nextSiblingId;

    internal void SetFirstChild(Guid? firstChildId) => FirstChildId = firstChildId;

    internal void MoveTo(Guid? parentBlockId, string sortKey, Guid? nextSiblingId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);

        ParentBlockId = parentBlockId;
        SortKey = sortKey;
        NextSiblingId = nextSiblingId;
    }

    internal void Rekey(string sortKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);

        SortKey = sortKey;
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
