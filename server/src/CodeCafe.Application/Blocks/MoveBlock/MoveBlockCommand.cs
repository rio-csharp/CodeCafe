using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Blocks.MoveBlock;

public sealed record MoveBlockCommand(Guid PageId, Guid BlockId, Guid? AfterBlockId) : ICommand<Result>;
