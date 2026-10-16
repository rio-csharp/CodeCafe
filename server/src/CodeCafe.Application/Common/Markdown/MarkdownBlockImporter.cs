using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Application.Revisions.Shared;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Common.Markdown;

// Recreates a parsed block tree under a page, level by level, in two passes. The model holds two
// pairs of two-way foreign keys — Page.FirstBlockId against Block.PageId, and Block.FirstChildId
// against Block.ParentBlockId — and EF cannot order the inserts of rows that reference each other
// inside one SaveChanges. So CreateBlocks writes the rows (page and parent only, no chain
// pointers) and the caller saves them, after which LinkChains sets every pointer as an UPDATE.
internal static class MarkdownBlockImporter
{
    public static IReadOnlyList<ImportedBlock> CreateBlocks(
        Page page,
        IReadOnlyList<ParsedMarkdownBlock> parsed,
        Block? parent,
        Guid batchId,
        IBlockRepository blocks,
        IBlockRevisionRepository revisions,
        RevisionSource source
    )
    {
        var created = new List<ImportedBlock>(parsed.Count);
        var siblings = new List<Block>();
        foreach (var parsedBlock in parsed)
        {
            var block = Block.Create(
                page.Id,
                parent?.Id,
                parsedBlock.Type,
                parsedBlock.Content.GetRawText(),
                parsedBlock.PlainText,
                BlockSiblingSortKeys.KeyForInsert(siblings, siblings.Count)
            );
            siblings.Add(block);
            blocks.Add(block);
            revisions.Add(RevisionRecording.Added(block, batchId, source));
            created.Add(
                new ImportedBlock(
                    block,
                    CreateBlocks(page, parsedBlock.Children, block, batchId, blocks, revisions, source)
                )
            );
        }

        return created;
    }

    // The second pass runs once the rows exist, so every chain pointer is an UPDATE and the FK
    // cycles no longer constrain the order. BlockChain.Insert stays the only writer of the chains.
    public static void LinkChains(Page page, IReadOnlyList<ImportedBlock> imported, Block? parent)
    {
        var siblings = new List<Block>();
        foreach (var node in imported)
        {
            BlockChain.Insert(node.Block, page, parent, siblings.Count > 0 ? siblings[^1] : null, next: null);
            siblings.Add(node.Block);
            LinkChains(page, node.Children, node.Block);
        }
    }
}

// One created block plus its created children, carried from the row pass to the chain pass.
internal sealed record ImportedBlock(Block Block, IReadOnlyList<ImportedBlock> Children);
