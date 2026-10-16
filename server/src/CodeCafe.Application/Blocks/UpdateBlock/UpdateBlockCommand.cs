using System.Text.Json;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Blocks.UpdateBlock;

public sealed record UpdateBlockCommand(
    Guid PageId,
    Guid BlockId,
    JsonElement Content,
    long BaseVersion) : ICommand<Result<BlockDto>>;
