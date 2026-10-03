using System.Text.Json;

using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Revisions.Shared;

// Builds revision rows from live blocks, so the five write handlers (and the two restore
// handlers) never assemble snapshots themselves. Every row of one logical change shares the
// caller's batchId.
public static class RevisionRecording
{
    public static BlockRevision Added(Block block, Guid batchId, RevisionSource source)
        => Row(block, BlockChangeKind.Added, batchId, source, subtreeJson: null);

    public static BlockRevision Updated(Block block, Guid batchId, RevisionSource source)
        => Row(block, BlockChangeKind.Updated, batchId, source, subtreeJson: null);

    public static BlockRevision Moved(Block block, Guid batchId, RevisionSource source)
        => Row(block, BlockChangeKind.Moved, batchId, source, subtreeJson: null);

    // One Deleted row per doomed node (page restore reconstructs from these); each subtree ROOT
    // — a doomed node whose parent survives — additionally carries the subtree snapshot JSON, so
    // a single-block restore can bring back everything deleted with it. Reads only
    // ParentBlockId/SortKey/payloads, which DeleteSubtree deliberately leaves intact.
    public static IReadOnlyList<BlockRevision> Deleted(IReadOnlyList<Block> doomed, Guid batchId, RevisionSource source)
    {
        var doomedIds = new HashSet<Guid>(doomed.Select(block => block.Id));
        return doomed
            .Select(block => Row(
                block,
                BlockChangeKind.Deleted,
                batchId,
                source,
                block.ParentBlockId is null || !doomedIds.Contains(block.ParentBlockId.Value)
                    ? SerializeSubtree(block, doomed, doomedIds)
                    : null
            ))
            .ToList();
    }

    private static BlockRevision Row(Block block, BlockChangeKind changeKind, Guid batchId, RevisionSource source, string? subtreeJson)
        => BlockRevision.Record(
            block.PageId,
            block.Id,
            batchId,
            block.Version,
            changeKind,
            source,
            block.ParentBlockId,
            block.SortKey,
            block.Type,
            block.ContentJson,
            block.PlainText,
            subtreeJson
        );

    // { id, type, content, plainText, children: [...] }, children in sibling (sort key) order.
    private static string SerializeSubtree(Block root, IReadOnlyList<Block> doomed, HashSet<Guid> doomedIds)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteNode(writer, root, doomed, doomedIds);
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteNode(Utf8JsonWriter writer, Block block, IReadOnlyList<Block> doomed, HashSet<Guid> doomedIds)
    {
        writer.WriteStartObject();
        writer.WriteString("id", block.Id);
        writer.WriteString("type", block.Type);
        writer.WritePropertyName("content");
        using (var content = JsonDocument.Parse(block.ContentJson))
        {
            content.RootElement.WriteTo(writer);
        }

        writer.WriteString("plainText", block.PlainText);
        writer.WritePropertyName("children");
        writer.WriteStartArray();
        foreach (var child in doomed
            .Where(candidate => candidate.ParentBlockId == block.Id)
            .OrderBy(candidate => candidate.SortKey, StringComparer.Ordinal))
        {
            WriteNode(writer, child, doomed, doomedIds);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
