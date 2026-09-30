using System.ComponentModel;
using System.Text.Json;
using CodeCafe.Application.Blocks.ApplyBlockOps;
using CodeCafe.Application.Blocks.DeleteBlock;
using CodeCafe.Application.Blocks.InsertBlocks;
using CodeCafe.Application.Blocks.MoveBlock;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.UpdateBlock;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeCafe.Host.Mcp.Tools;

[McpServerToolType]
public sealed class BlocksMcpTools
{
    [McpServerTool(Name = "codecafe_insert_blocks")]
    [Description("Insert new blocks into a page, positioned after the given block (or at the start when omitted).")]
    public static async Task<CallToolResult> InsertBlocks(
        ISender sender,
        Guid pageId,
        IReadOnlyList<BlockInput> blocks,
        CancellationToken cancellationToken,
        Guid? afterBlockId = null)
        => McpToolResults.From(await sender.Send(new InsertBlocksCommand(pageId, afterBlockId, blocks), cancellationToken));

    [McpServerTool(Name = "codecafe_apply_block_ops")]
    [Description("Apply a batch of block operations (insert/update/delete/move) to a page in one call; set dryRun to validate and preview the normalized ops without persisting.")]
    public static async Task<CallToolResult> ApplyBlockOps(
        ISender sender,
        Guid pageId,
        IReadOnlyList<BlockOp> ops,
        CancellationToken cancellationToken,
        bool dryRun = false)
        => McpToolResults.From(await sender.Send(new ApplyBlockOpsCommand(pageId, ops, dryRun), cancellationToken));

    [McpServerTool(Name = "codecafe_update_block")]
    [Description("Replace the content of one block. Requires the block's current revision for optimistic concurrency.")]
    public static async Task<CallToolResult> UpdateBlock(
        ISender sender,
        Guid pageId,
        Guid blockId,
        JsonElement content,
        long baseRevision,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new UpdateBlockCommand(pageId, blockId, content, baseRevision), cancellationToken));

    [McpServerTool(Name = "codecafe_move_block", Idempotent = true)]
    [Description("Move a block so it sits after the given block (or at the start when omitted).")]
    public static async Task<CallToolResult> MoveBlock(
        ISender sender,
        Guid pageId,
        Guid blockId,
        CancellationToken cancellationToken,
        Guid? afterBlockId = null)
        => McpToolResults.From(await sender.Send(new MoveBlockCommand(pageId, blockId, afterBlockId), cancellationToken));

    [McpServerTool(Name = "codecafe_delete_block", Destructive = true, Idempotent = true)]
    [Description("Delete a block from a page.")]
    public static async Task<CallToolResult> DeleteBlock(
        ISender sender,
        Guid pageId,
        Guid blockId,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new DeleteBlockCommand(pageId, blockId), cancellationToken));
}
