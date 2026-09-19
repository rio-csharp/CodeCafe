using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Blocks.DeleteBlock;

public sealed record DeleteBlockCommand(Guid PageId, Guid BlockId) : ICommand<Result>;
