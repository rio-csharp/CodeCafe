using System.Text.Json;
using CodeCafe.Application.Blocks.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Blocks.Commands;

public sealed record UpdateBlockCommand(
    Guid PageId,
    Guid BlockId,
    JsonElement Content,
    long BaseRevision) : ICommand<Result<BlockDto>>;
