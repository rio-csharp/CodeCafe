using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Application.Revisions.Shared;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Common.Markdown;

// Recreates a parsed block tree under a page level by level: each level chains its siblings and
// hands the head pointer to the parent (or the page at the top level) via BlockChain.Insert.
internal static class MarkdownBlockImporter
{
    public static void CreateBlocks(
        Page page,
        IReadOnlyList<ParsedMarkdownBlock> parsed,
        Block? parent,
        Guid batchId,
        IBlockRepository blocks,
        IBlockRevisionRepository revisions,
        RevisionSource source
    )
    {
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
            BlockChain.Insert(block, page, parent, siblings.Count > 0 ? siblings[^1] : null, next: null);
            siblings.Add(block);
            blocks.Add(block);
            revisions.Add(RevisionRecording.Added(block, batchId, source));
            CreateBlocks(page, parsedBlock.Children, block, batchId, blocks, revisions, source);
        }
    }
}
