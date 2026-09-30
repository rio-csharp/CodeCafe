using System.Text.Json;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Blocks.Shared;

public static class BlockMapping
{
    // Deserializing to JsonElement keeps the document alive, so the DTO can hand the payload to
    // the response serializer without another parse.
    public static BlockDto ToDto(Block block)
        => new(
            block.Id,
            block.ParentBlockId,
            block.Type,
            JsonSerializer.Deserialize<JsonElement>(block.ContentJson),
            block.SortKey,
            block.Revision,
            block.UpdatedAtUtc
        );

    // DFS pre-order for page reads: each parent immediately precedes its children, and every
    // sibling group comes out in chain order (the structural truth), starting at the page's top
    // level. An explicit stack keeps deep trees off the call stack; children are pushed in
    // reverse so they pop in chain order.
    public static IReadOnlyList<BlockDto> ToDtos(Page page, IReadOnlyList<Block> blocks)
    {
        var ordered = new List<BlockDto>(blocks.Count);
        var pending = new Stack<Block>();
        PushAll(pending, BlockChain.OrderByChain(page, blocks, null));
        while (pending.Count > 0)
        {
            var block = pending.Pop();
            ordered.Add(ToDto(block));
            PushAll(pending, BlockChain.OrderByChain(page, blocks, block.Id));
        }

        return ordered;
    }

    private static void PushAll(Stack<Block> pending, IReadOnlyList<Block> siblings)
    {
        for (var i = siblings.Count - 1; i >= 0; i--)
        {
            pending.Push(siblings[i]);
        }
    }
}
